assert(Lcs, "Please add 'Lcs = require \"lua.models.lcs\"' at the top of this script")

-- settings
local DefaultFilename = "LuaData.txt"

-- helper functions
function WriteToFile(filename, content)
	local file = io.open(filename, "a")
    file:write(content .. "\n")
    file:close()
end

function ClearFile(filename)
	local file = io.open(filename, "w+")
	file:close()
end

-- Writer
local Writer = Lcs.class()

function Writer:init(filename)
	if filename == nil then
		filename = DefaultFilename
	end
	self.filename = filename
end

function Writer:WriteLine(content)
	WriteToFile(self.filename, content)
end

function Writer:Clear()
	ClearFile(self.filename)
end

return Writer