-- In-game console, derived with heavy modification from Steve Donovan's ilua.lua
-- I do hope we're allowed to use it though. If not I'll just rewrite the copied sections.
-- Original header:
----------------------
-- ilua.lua
-- A more friendly Lua interactive prompt
-- doesn't need '='
-- will try to print out tables recursively, subject to the pretty_print_limit value.
-- Steve Donovan, 2007
----------------------

class "ConsoleHud"
class "Interpreter"

require "string"

function ConsoleHud:__init(interpreter)
    self.active = false
    self.interpreter = interpreter
    self.inputHistory = {}
    self.inputHistoryIndex = 0
end

function ConsoleHud:registerEvents(gameState)
    local root = gameState:rootGUIWindow()
    local consoleWindow = root:getChild("ConsoleWindow")
    local inputArea = consoleWindow:getChild("TextEntry")
    local outputArea = consoleWindow:getChild("History")
    inputArea:registerEventHandler("TextAccepted", textAccepted)
    inputArea:registerKeyEventHandler( handleConsoleKey)
end

function ConsoleHud:update()
    local gameState = Engine:currentGameState()
    local root = gameState:rootGUIWindow()
    local consoleWindow = root:getChild("ConsoleWindow")
    local inputArea = consoleWindow:getChild("TextEntry")
    if Engine.keyboard:wasKeyPressed(Keyboard.KC_F11) then
        self.active = not self.active
        if self.active then
            consoleWindow:show()
            consoleWindow:enable()
            inputArea:setFocus()
        else
             -- inputArea captures ` before deactivation, we don't want that.
            -- text, _ = string.gsub(inputArea:getText(), "`", "")
            text = inputArea:getText()
            inputArea:setText(text)
            consoleWindow:disable()
            consoleWindow:hide()
        end
    elseif self.active then
        if Engine.keyboard:wasKeyPressed(Keyboard.KC_RETURN) then
            -- push line to interpreter
            local outputArea = consoleWindow:getChild("History")
            local line = inputArea:getText()
            self.interpreter:eval_lua(line)
            inputArea:setText("")
            outputArea:setText(self.interpreter.history)
            self.inputHistoryIndex = #self.inputHistory
            self.inputHistory[self.inputHistoryIndex + 1] = line
            self.inputHistoryIndex = self.inputHistoryIndex + 1
        elseif Engine.keyboard:wasKeyPressed(Keyboard.KC_UP) and self.inputHistoryIndex > 0 then
            self.inputHistoryIndex = self.inputHistoryIndex - 1
            inputArea:setText(self.inputHistory[self.inputHistoryIndex + 1])
        elseif Engine.keyboard:wasKeyPressed(Keyboard.KC_DOWN) and self.inputHistoryIndex < #self.inputHistory - 1 then
            self.inputHistoryIndex = self.inputHistoryIndex + 1
            inputArea:setText(self.inputHistory[self.inputHistoryIndex + 1])
        end
    end
end

function ConsoleHud:eval()
    -- push line to interpreter
    if not self.active then return end
    local gameState = Engine:currentGameState()
    local consoleWindow = gameState:rootGUIWindow():getChild("ConsoleWindow")
    local inputArea = consoleWindow:getChild("TextEntry")
    local outputArea = consoleWindow:getChild("History")
    local line = inputArea:getText()
    self.interpreter:eval_lua(line)
    inputArea:setText("")
    outputArea:setText(self.interpreter.history)
    self.inputHistoryIndex = #self.inputHistory
    self.inputHistory[self.inputHistoryIndex + 1] = line
    self.inputHistoryIndex = self.inputHistoryIndex + 1
end

function ConsoleHud:handleKeys(key)
    local consoleWindow = Engine:currentGameState():rootGUIWindow():getChild("ConsoleWindow")
    local inputArea = consoleWindow:getChild("TextEntry")
    if key == Keyboard.KC_F11 then
        if self.active then
            consoleWindow:disable()
            consoleWindow:hide()
        end
        
    end
end

function Interpreter:__init()
    self.pretty_print_limit = 20
    self.max_depth = 7
    self.table_clever = true
    self.prompt = '> '
    self.verbose = false
    self.strict = false
    -- suppress strict warnings
    _ = true

    -- imported global functions
    self.sub = string.sub
    self.match = string.match
    self.find = string.find
    self.push = table.insert
    self.pop = table.remove
    self.append = table.insert
    self.concat = table.concat
    self.floor = math.floor
    self.write = io.write 
    self.read = io.read 

    self.savef = nil
    self.collisions = {}
    self.G_LIB = {}
    self.declared = {}
    self.line_handler_fn = nil
    self.global_handler_fn = nil
    self.print_handlers = {}

    self.ilua = {}
    self.num_prec = nil
    self.num_all = nil

    self.jstack = {}

    self.history = ""

    -- functions available in scripts
    function self.ilua.precision(len,prec,all)
        if not len then num_prec = nil
        else
            num_prec = '%'..len..'.'..prec..'f'
        end
        num_all = all
    end 

    function self.ilua.table_options(t)
        if t.limit then self.pretty_print_limit = t.limit end
        if t.depth then self.max_depth = t.depth end
        if t.clever ~= nil then self.table_clever = t.clever end
    end

    -- inject @tbl into the global namespace
    function self.ilua.import(tbl,dont_complain,lib)
        lib = lib or '<unknown>'
        if type(tbl) == 'table' then
            for k,v in pairs(tbl) do
                local key = rawget(_G,k)
                -- NB to keep track of collisions!
                if key and k ~= '_M' and k ~= '_NAME' and k ~= '_PACKAGE' and k ~= '_VERSION' then
                    append(collisions,{k,lib,G_LIB[k]})
                end
                _G[k] = v
                G_LIB[k] = lib
            end
        end
        if not dont_complain and  #self.collisions > 0  then
            for i, coll in ipairs(self.collisions) do
                local name,lib,oldlib = coll[1],coll[2],coll[3]
                write('warning: ',lib,'.',name,' overwrites ')
                if oldlib then
                    self.write(oldlib,'.',name,'\n')
                else
                    self.write('global ',name,'\n')
                end
            end
        end
    end

    function self.ilua.print_handler(name,handler)
        self.print_handlers[name] = handler
    end

    function self.ilua.line_handler(handler)
        self.line_handler_fn = handler
    end

    function self.ilua.global_handler(handler)
        self.global_handler_fn = handler
    end

    function self.ilua.print_variables()
        for name,v in pairs(self.declared) do
            print(name,type(_G[name]))
        end
    end

    -- any import complaints?
    self.ilua.import()
    
    -- enable 'not declared' error
    if self.strict then 
        self:set_strict()
    end

end

function Interpreter:oprint(...)
    if self.savef then
        self.savef:write(table.concat({...},' '),'\n')
    end
    self.history = self.history .. table.concat({...}, ' ') .. "\n"
end

function Interpreter:join(tbl,delim,limit,depth)
    if not limit then limit = self.pretty_print_limit end
    if not depth then depth = self.max_depth end
    local n = #tbl
    local res = ''
    local k = 0
    -- very important to avoid disgracing ourselves with circular references...
    if #self.jstack > depth then
        return "..."
    end
    for i,t in ipairs(self.jstack) do
        if tbl == t then
            return "<self>"
        end
    end
    push(self.jstack,tbl)
    -- this is a hack to work out if a table is 'list-like' or 'map-like'
    -- you can switch it off with ilua.table_options {clever = false}
    local is_list
    if self.table_clever then
        local index1 = n > 0 and tbl[1]
        local index2 = n > 1 and tbl[2]
        is_list = index1 and index2
    end
    if is_list then
        for i,v in ipairs(tbl) do
            res = res..delim..self:val2str(v)
            k = k + 1
            if k > limit then
                res = res.." ... "
                break
            end
        end
    else
        for key,v in pairs(tbl) do
            if type(key) == 'number' then
                key = '['..tostring(key)..']'
            else
                key = tostring(key)
            end
            res = res..delim..key..'='..self:val2str(v)
            k = k + 1
            if k > limit then
                res = res.." ... "
                break
            end            
        end
    end
    pop(self.jstack)
    return sub(res,2)
end

function Interpreter:val2str(val)
    local tp = type(val)
    if self.print_handlers[tp] then
        local s = self.print_handlers[tp](val)
        return s or '?'
    end
    if tp == 'function' then
        return tostring(val)
    elseif tp == 'table' then
        if val.__tostring  then
            return tostring(val)
        else
            return '{'..join(val,',')..'}'
        end
    elseif tp == 'string' then
        return "'"..val.."'"
    elseif tp == 'number' then
        -- we try only to apply floating-point precision for numbers deemed to be floating-point,
        -- unless the 3rd arg to precision() is true.
        if self.num_prec and (self.num_all or floor(val) ~= val) then
            return self.num_prec:format(val)
        else
            return tostring(val)
        end
    else
        return tostring(val)
    end
end

function Interpreter:_pretty_print(...)
    for i,val in ipairs({...}) do
        self:oprint(self:val2str(val))
    end
    _G['_'] = ({...})[1]
end

function Interpreter:compile(line)
    if self.verbose then self:oprint(line) end
    local f,err = load(line)
    return err,f
end

function Interpreter:evaluate(chunk)
    local ok,res = pcall(chunk)
    if not ok then
        return res
    end
    return nil -- meaning, fine!
end

function Interpreter:eval_lua(line)
    local oldprint = print
    print = oprint
    if self.savef then
        self.savef:write(prompt,line,'\n')
    end
    -- is the line handler interested?
    if self.line_handler_fn then
        line = self.line_handler_fn(line)
        -- returning nil here means that the handler doesn't want
        -- Lua to see the string
        if not line then return end
    end
    -- is it an expression?
    local err,chunk = self:compile('interpreter:_pretty_print('..line..')')
    if err then
        -- otherwise, a statement?
        err,chunk = self:compile(line)
    end
    -- if compiled ok, then evaluate the chunk
    if not err then
        err = self:evaluate(chunk)
    end
    -- if there was any error, print it out
    if err then
        self:oprint(err)
    end
    print = oldprint
end

function Interpreter:quit(code,msg)
    io.stderr:write(msg,'\n')
    -- os.exit(code)
end

--
-- strict.lua
-- checks uses of undeclared global variables
-- All global variables must be 'declared' through a regular assignment
-- (even assigning nil will do) in a main chunk before being used
-- anywhere.
--
function Interpreter:set_strict()
    local mt = getmetatable(_G)
    if mt == nil then
        mt = {}
        setmetatable(_G, mt)
    end

    local function what ()
        local d = debug.getinfo(3, "S")
        return d and d.what or "C"
    end

    mt.__newindex = function (t, n, v)
        self.declared[n] = true
        rawset(t, n, v)
    end
      
    mt.__index = function (t, n)
        if not self.declared[n] and what() ~= "C" then
            local lookup = self.global_handler_fn and self.global_handler_fn(n)
            if not lookup then
                error("variable '"..n.."' is not declared", 2)
            else
                return lookup
            end
        end
        return rawget(t, n)
    end

end

interpreter = Interpreter()

function oprint(...)
    interpreter:oprint(...)
end

function handleConsoleKey(window, key)
    console:handleKeys(key)
end

function textAccepted()
    console:eval()
end

console = ConsoleHud(interpreter)
Engine:registerConsoleObject(console)
