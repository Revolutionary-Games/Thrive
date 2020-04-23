FROM fedora:30

# mono repo
RUN rpm --import "https://keyserver.ubuntu.com/pks/lookup?op=get&search=0x3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF"
RUN curl https://download.mono-project.com/repo/centos8-stable.repo | tee /etc/yum.repos.d/mono-centos8-stable.repo

RUN dnf install -y --setopt=deltarpm=false ruby ruby-devel redhat-rpm-config git git-lfs p7zip tar xz gcc make mono-complete nuget wget && dnf clean all
RUN git lfs install
RUN gem install os colorize rubyzip json sha3 httparty

# Godot install
ENV GODOT_VERSION "3.2.1"

RUN wget https://downloads.tuxfamily.org/godotengine/${GODOT_VERSION}/mono/Godot_v${GODOT_VERSION}-stable_mono_linux_headless_64.zip \
    && wget https://downloads.tuxfamily.org/godotengine/${GODOT_VERSION}/mono/Godot_v${GODOT_VERSION}-stable_mono_export_templates.tpz \
    && mkdir .p ~/.cache \
    && mkdir -p ~/.config/godot \
    && mkdir -p ~/.local/share/godot/templates/${GODOT_VERSION}.stable.mono \
    && unzip Godot_v${GODOT_VERSION}-stable_mono_linux_headless_64.zip \
    && mv Godot_v${GODOT_VERSION}-stable_mono_linux_headless_64 godot_dir \
    && mv godot_dir/* /usr/local/bin/ \
    && ln -s /usr/local/bin/Godot_v${GODOT_VERSION}-stable_mono_linux_headless.64 /usr/local/bin/godot \
    && unzip Godot_v${GODOT_VERSION}-stable_mono_export_templates.tpz \
    && mkdir -p ~/.local/share/godot/templates/${GODOT_VERSION}.stable.mono \
    && mv templates/* ~/.local/share/godot/templates/${GODOT_VERSION}.stable.mono/ \
    && rm -f Godot_v${GODOT_VERSION}-stable_mono_export_templates.tpz Godot_v${GODOT_VERSION}-stable_mono_linux_headless_64.zip \
    && rm -rf templates godot_dir