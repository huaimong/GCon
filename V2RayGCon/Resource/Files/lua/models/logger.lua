local Lcs = require "lua.models.lcs"

-- settings
local DefaultLogFilename = "LuaLog.txt"

-- helper functions
function WriteLine(filename, content)
	local file=io.open(filename, "a")
    file:write(content .. "\n")
    file:close()
end

-- Logger
local Logger = Lcs.class()

function Logger:init(filename)
	if filename == nil then
		filename = DefaultLogFilename
	end
	self.filename = filename
end

function Logger:Warn(text)
	self:Log("Warn", text)
end

function Logger:Debug(text)
	self:Log("Debug", text)
end

function Logger:Error(text)
	self:Log("Error", text)
end

function Logger:Info(text)
	self:Log("Info", text)
end

function Logger:Log(prefix, content)
	if prefix == nil then
		prefix = ""
	else
		prefix = " [" .. prefix .. "] "
	end
	local timestamp = os.date("[%Y-%m-%d %X]")
	local line = timestamp .. prefix .. content
	WriteLine(self.filename, line)
end

return Logger