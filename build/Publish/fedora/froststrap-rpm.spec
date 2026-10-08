Name:           leafstrap
Version:        %{?leafstrap_version}%{!?leafstrap_version:1.0.6}
Release:        1%{?dist}
Summary:        %description

License:        MPL-2.0
URL:            https://github.com/Ieavemealone012/leafstrap
BuildArch:      x86_64
Requires:       libicu

%description
A cross-platform Roblox bootstrapper focused on customization

%global __brp_strip /bin/true
%global __brp_strip_comment_note /bin/true

%prep

%build

%install
rm -rf %{buildroot}
mkdir -p %{buildroot}
cp -a %{_leafstrap_appdir}/usr %{buildroot}/

%files
/usr/bin/Leafstrap
/usr/share/applications/Leafstrap.desktop
/usr/share/icons/hicolor/512x512/apps/leafstrap.png

%post
if [ -x /usr/bin/update-desktop-database ]; then
    /usr/bin/update-desktop-database -q /usr/share/applications || :
fi
if [ -x /usr/bin/gtk-update-icon-cache ]; then
    /usr/bin/gtk-update-icon-cache -q /usr/share/icons/hicolor || :
fi
/usr/bin/Leafstrap --register-mime-types 2>/dev/null || :

%postun
if [ -x /usr/bin/update-desktop-database ]; then
    /usr/bin/update-desktop-database -q /usr/share/applications || :
fi
if [ -x /usr/bin/gtk-update-icon-cache ]; then
    /usr/bin/gtk-update-icon-cache -q /usr/share/icons/hicolor || :
fi
