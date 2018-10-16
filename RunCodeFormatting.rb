#!/usr/bin/env ruby
# This script goes through all the thrive source files and makes sure that they are formatted
# correctly
# TODO: also add syntax based checks for angelscript
require 'find'
require 'optparse'

options = {}
OptionParser.new do |opts|
  opts.on("--only-js", "Only checks that javascript (and CSS) files are good") do |b|
    options[:onlyJS] = b
  end
end.parse!

if !ARGV.empty?
  puts "Invalid parameters given: unparsed: #{ARGV}"
  exit 2
end

if !options[:onlyJS]
  file_paths = []
  Find.find('.') do |path|

    begin
      if path !~ /\.h(pp)?$/ && path !~ /\.cpp$/

        if path =~ /\.as$/
          # Check that angelscript doesn't have lines starting with tabs
          begin
            original = File.read(path)
            fixedASCode = ""

            original.each_line{|line|
              if line.empty?
                fixedASCode += "\n"
              else

                # Remove trailing whitespace
                line.rstrip!
                line += "\n"

                replaced = line.gsub(/^(?:(?!\t)\s)*\t/, "    ")

                # Run while it matches
                while replaced != line
                  line = replaced
                  replaced = line.gsub(/^(?:(?!\t)\s)*\t/, "    ")
	            end
                
                fixedASCode += replaced
              end
            }
            
            if original != fixedASCode
              puts "AngelScript file fixed: #{path}"
              File.write(path, fixedASCode)
            end
            
          rescue ArgumentError => e
            abort "AngelScript file isn't utf8 encoded: " + path + ", e: #{e}"
          end
        end
        
        next
      end
    rescue ArgumentError => e

      puts "Failed to handle path: " + path
      puts "Error: " + e.message
      raise e
    end

    if path !~ /\/src\//i && path !~ /\/test\//i ||
       # Generated files
       path =~ /\/generated\//i || path =~ /\/src\/main.cpp/i ||
       path =~ /\/src\/thrive_version.h/i ||
       # ignore catch
       path =~ /catch.hpp$/i
      
      next
    end

    system "clang-format", "-i", path
    abort("\n\nFAILED to format file: " + path) if $?.exitstatus != 0
  end
end

# JavaScript linting
system(%{eslint "scripts/gui/**/*.*js" "scripts/gui/**/*.html" --fix})
abort("\nJavaScript style errors were found.") if $?.exitstatus != 0

# Stylelint for css
system(%{stylelint scripts/gui/**/*.css scripts/gui/**/*.html --fix})
abort("\nCSS style errors were found.") if $?.exitstatus != 0

