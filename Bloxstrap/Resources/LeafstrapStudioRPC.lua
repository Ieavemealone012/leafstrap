-- LeafstrapStudioRPC SDK v1.3.0

local Selection = game:GetService("Selection")
local RunService = game:GetService("RunService")
local HttpService = game:GetService("HttpService")
local MarketplaceService = game:GetService("MarketplaceService")
local Players = game:GetService("Players")
local StudioService = game:GetService("StudioService")
local ScriptEditorService = game:GetService("ScriptEditorService")

local LeafstrapStudioRPC = {}
local Plugin = if plugin then
	plugin
	else 
	nil

LeafstrapStudioRPC.Config = {
	enabled = true,
	httpTimeout = 5,
	retryCount = 4,
	retryDelay = 1,
	updateInterval = 10,
	port = 4875
}

local State = {
	lastPayload = {},
	isCooldown = false,
	isInitialized = false,
	monitoringThread = nil,
	connections = {},
	cachedWorkspace = { name = "Unsaved Studio Project", isPublic = false }
}

local ScriptTypes = {
	SERVER = "Server Script",
	CLIENT = "Local Script",
	SERVER_MODULE = "Server Module",
	CLIENT_MODULE = "Client Module",
	MODULE = "Module",
	DEVELOPING = "Developing"
}

local function clearConnections()
	for _, connection in pairs(State.connections) do
		if connection then connection:Disconnect() end
	end
	table.clear(State.connections)
end

local function getScriptLineCount(scriptObj)
	if not scriptObj or not scriptObj:IsA("LuaSourceContainer") then return 0 end
	local success, source = pcall(function() return ScriptEditorService:GetEditorSource(scriptObj) end)
	if not success or not source then
		success, source = pcall(function() return scriptObj.Source end)
	end
	if not success or not source then return 0 end
	local _, count = source:gsub("\n", "")
	return count + 1
end

local function getScriptType(scriptObj)
	if not scriptObj then return ScriptTypes.DEVELOPING end

	if scriptObj:IsA("LocalScript") or (scriptObj:IsA("Script") and scriptObj.RunContext == Enum.RunContext.Client) then
		return ScriptTypes.CLIENT
	end

	if scriptObj:IsA("ModuleScript") then
		local ancestor = scriptObj.Parent
		while ancestor do
			if ancestor:IsA("ServerScriptService") or ancestor:IsA("ServerStorage") then
				return ScriptTypes.SERVER_MODULE
			elseif ancestor:IsA("StarterPlayer") or ancestor:IsA("StarterGui") or ancestor:IsA("StarterPack") then
				return ScriptTypes.CLIENT_MODULE
			end
			ancestor = ancestor.Parent
		end
		return ScriptTypes.MODULE
	end

	if scriptObj:IsA("Script") then
		return ScriptTypes.SERVER
	end

	return ScriptTypes.DEVELOPING
end

local function refreshWorkspaceCache()
	if game.PlaceId > 0 then
		local success, info = pcall(function() return MarketplaceService:GetProductInfoAsync(game.PlaceId) end)
		if success and info then
			State.cachedWorkspace.name = info.Name or "Published Place"
			State.cachedWorkspace.isPublic = true
			return
		end
	end
	State.cachedWorkspace.name = (game.Name ~= "Place" and game.Name ~= "") and game.Name or "Unsaved Studio Project"
	State.cachedWorkspace.isPublic = false
end

local function sendViaHTTP(payload)
	local httpSuccess, httpEnabled = pcall(function() return HttpService.HttpEnabled end)
	if httpSuccess and not httpEnabled then
		warn("Leafstrap Studio RPC: HTTP Requests are disabled. Enable them in Experience Settings -> Security.")
		return false
	end

	local url = string.format("http://localhost:%d/rpc", LeafstrapStudioRPC.Config.port)
	local body = HttpService:JSONEncode(payload)
	local lastError = "Unknown error"

	for attempt = 1, LeafstrapStudioRPC.Config.retryCount do
		local success, response = pcall(function()
			return HttpService:RequestAsync({
				Url = url,
				Method = "POST",
				Headers = {["Content-Type"] = "application/json"},
				Body = body,
				Timeout = LeafstrapStudioRPC.Config.httpTimeout
			})
		end)

		if success and response and response.Success then
			return true
		end

		if success and response then
			lastError = string.format("HTTP %s: %s", tostring(response.StatusCode), tostring(response.StatusMessage))
		else
			lastError = tostring(response)
		end

		if attempt < LeafstrapStudioRPC.Config.retryCount then
			task.wait(LeafstrapStudioRPC.Config.retryDelay * attempt)
		end
	end

	warn(string.format("Leafstrap Studio RPC: Could not reach Leafstrap after %d attempts (%s)", LeafstrapStudioRPC.Config.retryCount, lastError))
	return false
end

function LeafstrapStudioRPC.SendMessage(data)
	if State.isCooldown then return end

	local isRedundant = true
	for key, value in pairs(data) do
		if State.lastPayload[key] ~= value then
			isRedundant = false
			break
		end
	end
	if isRedundant then return end

	State.isCooldown = true
	task.spawn(function()
		local delivered = sendViaHTTP({
			command = "SetRichPresence",
			data = data
		})

		-- Only suppress duplicates after Leafstrap confirms receipt. A timed-out
		-- payload remains eligible for the next periodic update.
		if delivered then
			State.lastPayload = data
		end
		State.isCooldown = false
	end)
end

function LeafstrapStudioRPC.UpdatePresence()
	if not LeafstrapStudioRPC.Config.enabled then return end

	local activeScript = StudioService.ActiveScript
	local scriptObj = (activeScript and activeScript:IsA("LuaSourceContainer")) and activeScript or nil

	if not scriptObj then
		local selected = Selection:Get()
		if #selected == 1 and selected[1]:IsA("LuaSourceContainer") then
			scriptObj = selected[1]
		end
	end

	local scriptType = getScriptType(scriptObj)
	local stateText = scriptObj and string.format("Editing %s (%d lines)", scriptObj.Name, getScriptLineCount(scriptObj)) or "Idling in Studio"

	LeafstrapStudioRPC.SendMessage({
		details = State.cachedWorkspace.name,
		state = stateText,
		testing = RunService:IsRunning(),
		scriptType = scriptType,
		placeId = game.PlaceId,
		isPublic = State.cachedWorkspace.isPublic,
		devCount = math.max(1, #Players:GetPlayers())
	})
end

function LeafstrapStudioRPC.SetEnabled(enabled)
	LeafstrapStudioRPC.Config.enabled = enabled

	task.spawn(sendViaHTTP, {
		command = "RPCToggle",
		data = { 
			enabled = enabled, 
			workspace = State.cachedWorkspace.name, 
			isPublic = State.cachedWorkspace.isPublic 
		}
	})

	if enabled then
		refreshWorkspaceCache()
		LeafstrapStudioRPC.UpdatePresence()
		if not State.monitoringThread then
			State.monitoringThread = task.spawn(function()
				while LeafstrapStudioRPC.Config.enabled do
					task.wait(LeafstrapStudioRPC.Config.updateInterval)
					LeafstrapStudioRPC.UpdatePresence()
				end
				State.monitoringThread = nil
			end)
		end
	else
		State.monitoringThread = nil
	end
end

function LeafstrapStudioRPC.Initialize()
	if State.isInitialized or not Plugin then return end

	local httpSuccess, httpEnabled = pcall(function() return HttpService.HttpEnabled end)
	if httpSuccess and not httpEnabled then
		warn("Leafstrap Studio RPC: HTTP Requests are disabled. Enable them in Experience Settings -> Security.")
	end

	local toggleAction = Plugin:CreatePluginAction(
		"toggle_leafstrap_rpc_command", "Toggle Leafstrap Studio RPC", "Toggle Leafstrap Studio Discord Rich Presence", "rbxassetid://111400040119373", true
	)

	State.connections.Toggle = toggleAction.Triggered:Connect(function()
		LeafstrapStudioRPC.SetEnabled(not LeafstrapStudioRPC.Config.enabled)
	end)

	State.connections.UpdateTrigger = Selection.SelectionChanged:Connect(function()
		task.defer(LeafstrapStudioRPC.UpdatePresence)
	end)

	State.connections.TabSwitch = StudioService:GetPropertyChangedSignal("ActiveScript"):Connect(function()
		task.defer(LeafstrapStudioRPC.UpdatePresence)
	end)

	State.connections.Unload = Plugin.Unloading:Connect(function()
		LeafstrapStudioRPC.SetEnabled(false)
		clearConnections()
	end)

	refreshWorkspaceCache()
	LeafstrapStudioRPC.SetEnabled(LeafstrapStudioRPC.Config.enabled)
	State.isInitialized = true
end

if Plugin then LeafstrapStudioRPC.Initialize() end
return LeafstrapStudioRPC
