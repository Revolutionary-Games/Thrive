# A really hacky way to find libraries to link to by globbing
# all matching file names
require_relative 'RubySetupSystem'


class Globber
    def initialize(libname, path)
        # Force libName to be an array
        if libname.kind_of?(Array)
            @LibName = Array.new + libname
        else
            @LibName = [libname]
        end
        @Path = path
        
        @foundStatus = Array.new
        # This will contain all the files that were found
        @foundPaths = Array.new
    end
    
    attr_reader :LibName
    
    # Runs the globber. Returns true if all were found, false if not
    def run()
        
        @LibName.each do |lib|
            info "Looking for library #{lib}"
           @foundStatus.push findLibrary(lib)
        end
        
        @foundStatus.each do |found|
            if not found
                return false
            end
        end
        
        info "All libraries found"
        true
    end
    
    def findLibrary(filename)
        
        extension = File.extname filename
        
        Dir.glob("#{@Path}/**/*#{extension}") do |file|
            if File.basename(file) == filename
                info "Found lib: #{file}"
                @foundPaths.push file
                return true
            end
        end
        
        warning "library #{filename} not found in path: #{@Path}"
        # not found
        false
    end
    
    def getResult()
        @foundPaths
    end
end

