#!/usr/bin/env ruby
# Creates docker images for running Thrive CI inside them
require_relative 'RubySetupSystem/DockerImageCreator'

# There isn't an easy way to currently base our image on the Leviathan one, so we do this
require_relative 'ThirdParty/Leviathan/LeviathanLibraries'

runDockerCreate($leviathanLibList, $leviathanSelfLib, extraPackages: ["nodejs"],
                extraSteps: [
                  "RUN npm install -g eslint stylelint eslint-plugin-html http-server " +
                  "&& npm cache clean --force"
                ])
