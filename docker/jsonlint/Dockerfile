FROM fedora:36


RUN dnf install -y --setopt=deltarpm=false git git-lfs dotnet-sdk-6.0 python3 python3-pip gettext && dnf clean all
RUN git lfs install
COPY requirements.txt /requirements-thrive.txt
RUN pip3 --no-cache-dir install -r /requirements-thrive.txt
