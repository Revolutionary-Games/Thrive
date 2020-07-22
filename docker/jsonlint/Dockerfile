FROM fedora:32


RUN dnf install -y --setopt=deltarpm=false ruby ruby-devel redhat-rpm-config make gcc git git-lfs nodejs zlib-devel && dnf clean all
RUN git lfs install
RUN gem install os colorize rubyzip json sha3 httparty nokogiri
RUN npm install -g jsonlint && npm cache clean --force
