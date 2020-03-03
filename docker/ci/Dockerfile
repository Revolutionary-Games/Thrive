FROM fedora:30

# mono repo
RUN rpm --import "https://keyserver.ubuntu.com/pks/lookup?op=get&search=0x3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF"
RUN curl https://download.mono-project.com/repo/centos8-stable.repo | tee /etc/yum.repos.d/mono-centos8-stable.repo

RUN dnf install -y --setopt=deltarpm=false ruby ruby-devel redhat-rpm-config git git-lfs p7zip tar xz gcc make mono-complete nuget && dnf clean all
RUN git lfs install
RUN gem install os colorize rubyzip json sha3
