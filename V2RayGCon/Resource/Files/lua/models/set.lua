local Lcs = require "lua.models.lcs"

local Set = Lcs.class()

function Set:init()
	self.data = {}
end

function Set:Add(element)
	self.data[element] = true
end

function Set:Remove(element)
	self.data[element] = nil
end

function Set:Contains(element)
	return self.data[element] ~= nil
end

return Set