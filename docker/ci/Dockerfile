FROM fedora:34

RUN dnf install -y --setopt=deltarpm=false ruby ruby-devel redhat-rpm-config git git-lfs \
    p7zip tar unzip xz gcc make wget dotnet-sdk-5.0 dotnet-runtime-3.1 && dnf clean all
RUN git lfs install
RUN gem install os colorize rubyzip json sha3 httparty parallel nokogiri

# Jetbrains CLI tools
RUN wget https://download.jetbrains.com/resharper/dotUltimate.2021.2.1/JetBrains.ReSharper.CommandLineTools.2021.2.1.zip \
    -O jetbrains.zip && unzip jetbrains.zip -d /usr/bin && rm -rf jetbrains.zip && chmod +x /usr/bin/*.sh

# Godot install
ENV GODOT_VERSION "3.4"

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

# Editor settings to use the right build tool
COPY editor_settings-3.tres /root/.config/godot/editor_settings-3.tres

# Godot pck tool
ENV PCK_TOOL_VERSION "v1.4"

RUN wget https://github.com/hhyyrylainen/GodotPckTool/releases/download/${PCK_TOOL_VERSION}/godotpcktool -O /usr/bin/godotpcktool && \
    chmod +x /usr/bin/godotpcktool
