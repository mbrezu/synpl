#!/bin/bash

set -e

echo Updating the system.

apt-get update

apt-get install build-essential autoconf automake \
bison flex gtk-sharp2-gapi boo gdb valac libfontconfig1-dev \
libcairo2-dev libpango1.0-dev libfreetype6-dev libexif-dev \
libjpeg62-dev libtiff4-dev libgif-dev zlib1g-dev libatk1.0-dev \
libglib2.0-dev libgtk2.0-dev libglade2-dev libart-2.0-dev \
libgnomevfs2-dev libgnome2-dev libgnomecanvas2-dev \
libgnomeui-dev libgnomeprint2.2-dev libgnomeprintui2.2-dev \
libpanel-applet2-dev librsvg2-dev \
libgtkhtml3.14-dev libgtksourceview2.0-dev libgtksourceview-dev \
libvte-dev libwnck-dev libnspr4-dev libnss3-dev libxul-dev \
libwebkit-dev libvala-dev

echo Fetch the sources.

wget http://ftp.novell.com/pub/mono/sources/mono/mono-2.4.tar.bz2
wget http://ftp.novell.com/pub/mono/sources/libgdiplus/libgdiplus-2.4.tar.bz2
wget http://ftp.novell.com/pub/mono/sources/gluezilla/gluezilla-2.4.tar.bz2
wget http://ftp.novell.com/pub/mono/sources/xsp/xsp-2.4.tar.bz2
wget http://ftp.novell.com/pub/mono/sources/mono-tools/mono-tools-2.4.tar.bz2
wget http://ftp.novell.com/pub/mono/sources/gecko-sharp2/gecko-sharp-2.0-0.13.tar.bz2
wget http://ftp.novell.com/pub/mono/sources/mono-debugger/mono-debugger-2.4.tar.bz2
wget http://ftp.novell.com/pub/mono/sources/mono-addins/mono-addins-0.4.zip
wget http://ftp.novell.com/pub/mono/sources/gtk-sharp212/gtk-sharp-2.12.8.tar.bz2
wget http://ftp.novell.com/pub/mono/sources/gnome-sharp220/gnome-sharp-2.20.1.tar.bz2
wget http://ftp.novell.com/pub/mono/sources/gnome-desktop-sharp2/gnome-desktop-sharp-2.20.1.tar.bz2
wget http://ftp.novell.com/pub/mono/sources/webkit-sharp/webkit-sharp-0.2.tar.bz2
wget http://ftp.novell.com/pub/mono/sources/monodevelop/monodevelop-2.0.tar.bz2
wget http://ftp.novell.com/pub/mono/sources/monodevelop-debugger-mdb/monodevelop-debugger-mdb-2.0.tar.bz2
wget http://ftp.novell.com/pub/mono/sources/monodevelop-debugger-gdb/monodevelop-debugger-gdb-2.0.tar.bz2
wget http://ftp.novell.com/pub/mono/sources/monodevelop-database/monodevelop-database-2.0.tar.bz2
wget http://ftp.novell.com/pub/mono/sources/monodevelop-java/monodevelop-java-2.0.tar.bz2
wget http://ftp.novell.com/pub/mono/sources/monodevelop-vala/monodevelop-vala-2.0.tar.bz2

# Create the configuration files

cat > mono-2.4-environment <<EOF
#!/bin/bash
MONO_PREFIX=/opt/mono-2.4
GNOME_PREFIX=/opt/gnome-2.4
export DYLD_LIBRARY_PATH=\$MONO_PREFIX/lib:\$DYLD_LIBRARY_PATH
export LD_LIBRARY_PATH=\$MONO_PREFIX/lib:\$LD_LIBRARY_PATH
export C_INCLUDE_PATH=\$MONO_PREFIX/include:\$GNOME_PREFIX/include
export ACLOCAL_PATH=\$MONO_PREFIX/share/aclocal
export PKG_CONFIG_PATH=\$MONO_PREFIX/lib/pkgconfig:\$GNOME_PREFIX/lib/pkgconfig
PATH=\$MONO_PREFIX/bin:\$PATH
PS1="[mono-2.4] \w @ "
EOF

mv mono-2.4-environment /usr/local/bin
chmod +x /usr/local/bin/mono-2.4-environment

cat > mono-2.4 <<EOF
#!/bin/bash
MONO_PREFIX=/opt/mono-2.4
GNOME_PREFIX=/opt/gnome-2.4
export DYLD_LIBRARY_PATH=\$MONO_PREFIX/lib:\$DYLD_LIBRARY_PATH
export LD_LIBRARY_PATH=\$MONO_PREFIX/lib:\$LD_LIBRARY_PATH
export C_INCLUDE_PATH=\$MONO_PREFIX/include:\$GNOME_PREFIX/include
export ACLOCAL_PATH=\$MONO_PREFIX/share/aclocal
export PKG_CONFIG_PATH=\$MONO_PREFIX/lib/pkgconfig:\$GNOME_PREFIX/lib/pkgconfig
PATH=\$MONO_PREFIX/bin:\$PATH

exec "\$@"
EOF

mv mono-2.4 /usr/local/bin
chmod +x /usr/local/bin/mono-2.4

MONO_PREFIX=/opt/mono-2.4
GNOME_PREFIX=/opt/gnome-2.4
export DYLD_LIBRARY_PATH=$MONO_PREFIX/lib:$DYLD_LIBRARY_PATH
export LD_LIBRARY_PATH=$MONO_PREFIX/lib:$LD_LIBRARY_PATH
export C_INCLUDE_PATH=$MONO_PREFIX/include:$GNOME_PREFIX/include
export ACLOCAL_PATH=$MONO_PREFIX/share/aclocal
export PKG_CONFIG_PATH=$MONO_PREFIX/lib/pkgconfig:$GNOME_PREFIX/lib/pkgconfig
PATH=$MONO_PREFIX/bin:$PATH
PS1="[mono-2.4] \w @ "

rm -rf /opt/mono-2.4
mkdir /opt/mono-2.4

echo Unzip the sources.

for i in `ls *.tar.bz2`
do
  echo $i
  tar -xf $i
done

unzip mono-addins-0.4.zip

echo Compiling.

cd libgdiplus-2.4
./configure --prefix=/opt/mono-2.4 --with-pango
make
make install

cd ../mono-2.4
./configure --prefix=/opt/mono-2.4
make
make install

cd ../gtk-sharp-2.12.8
./configure --prefix=/opt/mono-2.4
make
make install

cd ../gnome-sharp-2.20.1
./configure --prefix=/opt/mono-2.4
make
make install

cd ../gnome-desktop-sharp-2.20.1
./configure --prefix=/opt/mono-2.4
make
make install

cd ../gecko-sharp-2.0-0.13
./configure --prefix=/opt/mono-2.4
make
make install

cd ../webkit-sharp-0.2
./configure --prefix=/opt/mono-2.4
make
make install

cd ../mono-addins-0.4
./configure --prefix=/opt/mono-2.4
make
make install

cd ../mono-tools-2.4
./configure --prefix=/opt/mono-2.4
make
make install

cd ../xsp-2.4
./configure --prefix=/opt/mono-2.4
make
make install

cd ../mono-debugger-2.4
./configure --prefix=/opt/mono-2.4
make
make install

cd ../monodevelop-2.0
./configure --prefix=/opt/mono-2.4
make
make install

cd ../monodevelop-debugger-mdb-2.0
./configure --prefix=/opt/mono-2.4
make
make install

cd ../monodevelop-debugger-gdb-2.0
./configure --prefix=/opt/mono-2.4
make
make install

cd ../monodevelop-database-2.0
./configure --prefix=/opt/mono-2.4
make
make install

cd ../monodevelop-java-2.0
./configure --prefix=/opt/mono-2.4
make
make install

cd ../monodevelop-vala-2.0
./configure --prefix=/opt/mono-2.4
make
make install
