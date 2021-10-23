FROM fedora:34


RUN dnf install -y --setopt=deltarpm=false ruby ruby-devel redhat-rpm-config make gcc git git-lfs nodejs zlib-devel python3 python3-pip gettext && dnf clean all
RUN git lfs install
RUN gem install os colorize rubyzip json sha3 httparty nokogiri
RUN npm install -g jsonlint && npm cache clean --force
COPY requirements.txt /requirements-thrive.txt
RUN pip3 --no-cache-dir install -r /requirements-thrive.txt
