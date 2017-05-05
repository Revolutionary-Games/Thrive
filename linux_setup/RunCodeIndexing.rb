#!/usr/bin/env ruby

require 'find'


if File.exists? "src"
  runFolder = "."
else
  runFolder = "../"
end

Dir.chdir(runFolder){

  file_paths = []
  Find.find('.') do |path|
    file_paths << path if (not path =~ /.*\/contrib\/.*/) and
      (not path =~ /.*\/build\/.*/) and ((path =~ /.*\.h/) or
                                         (path =~ /.*\.cpp/))
  end

  File.open("cscope.files", "w") do |f|
    f.puts(file_paths)
  end

  system "cscope -b"
}
