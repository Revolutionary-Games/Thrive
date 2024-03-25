FROM fedora:39

RUN dnf install -y --setopt=deltarpm=false git git-lfs unzip wget dotnet-sdk-8.0 \
    dotnet-runtime-6.0 gettext p7zip && dnf clean all
RUN git lfs install

# Godot install
# ENV GODOT_VERSION "4.2.1"

# RUN wget https://github.com/godotengine/godot/releases/download/${GODOT_VERSION}-stable/Godot_v${GODOT_VERSION}-stable_mono_linux_x86_64.zip \
#     && wget https://github.com/godotengine/godot/releases/download/${GODOT_VERSION}-stable/Godot_v${GODOT_VERSION}-stable_mono_export_templates.tpz \
#     && mkdir .p ~/.cache \
#     && mkdir -p ~/.config/godot \
#     && mkdir -p ~/.local/share/godot/export_templates/${GODOT_VERSION}.stable.mono \
#     && unzip Godot_v${GODOT_VERSION}-stable_mono_linux_x86_64.zip \
#     && mv Godot_v${GODOT_VERSION}-stable_mono_linux_x86_64 godot_dir \
#     && mv godot_dir/* /usr/local/bin/ \
#     && ln -s /usr/local/bin/Godot_v${GODOT_VERSION}-stable_mono_linux.x86_64 /usr/local/bin/godot \
#     && unzip Godot_v${GODOT_VERSION}-stable_mono_export_templates.tpz \
#     && mkdir -p ~/.local/share/godot/export_templates/${GODOT_VERSION}.stable.mono \
#     && mv templates/* ~/.local/share/godot/export_templates/${GODOT_VERSION}.stable.mono/ \
#     && rm -f Godot_v${GODOT_VERSION}-stable_mono_export_templates.tpz Godot_v${GODOT_VERSION}-stable_mono_linux_x86_64.zip \
#     && rm -rf templates godot_dir

# Temporary RC install
ENV GODOT_VERSION "4.2.2-rc2"

RUN wget https://downloads.tuxfamily.org/godotengine/4.2.2/rc2/mono/Godot_v4.2.2-rc2_mono_linux_x86_64.zip \
    && wget https://downloads.tuxfamily.org/godotengine/4.2.2/rc2/mono/Godot_v4.2.2-rc2_mono_export_templates.tpz \
    && mkdir .p ~/.cache \
    && mkdir -p ~/.config/godot \
    && mkdir -p ~/.local/share/godot/export_templates/${GODOT_VERSION}.rc2.mono \
    && unzip Godot_v${GODOT_VERSION}_mono_linux_x86_64.zip \
    && mv Godot_v${GODOT_VERSION}_mono_linux_x86_64 godot_dir \
    && mv godot_dir/* /usr/local/bin/ \
    && ln -s /usr/local/bin/Godot_v${GODOT_VERSION}_mono_linux.x86_64 /usr/local/bin/godot \
    && unzip Godot_v${GODOT_VERSION}_mono_export_templates.tpz \
    && mkdir -p ~/.local/share/godot/export_templates/${GODOT_VERSION}.rc2.mono \
    && mv templates/* ~/.local/share/godot/export_templates/${GODOT_VERSION}.rc2.mono/ \
    && rm -f Godot_v${GODOT_VERSION}_mono_export_templates.tpz Godot_v${GODOT_VERSION}_mono_linux_x86_64.zip \
    && rm -rf templates godot_dir

# Godot pck tool
ENV PCK_TOOL_VERSION "v2.0"

RUN wget https://github.com/hhyyrylainen/GodotPckTool/releases/download/${PCK_TOOL_VERSION}/godotpcktool -O /usr/bin/godotpcktool && \
    chmod +x /usr/bin/godotpcktool
