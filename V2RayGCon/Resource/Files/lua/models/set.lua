local Lcs = require "lua.models.lcs"

local Set = Lcs.class()

function Set:init(data)
	self.data = {}
	if data == nil then
		return
	end
	for key, item in pairs(data) do
		self.data[item] = true
	end
end

function Set:Count()
	local count = 0
	for key, item in pairs(self.data) do	
		-- print("data ", key, " = ", item)
		if item == true then
			count = count + 1
		end
	end
	return count
end

function Set:Reset(data)
	self.data = {}
	if data == nil then
		return
	end
	for key, item in pairs(data) do
		-- print("set ", key, " = ", item)
		self.data[item] = true
	end
end

function Set:Add(element)
	self.data[element] = true
end

function Set:Remove(element)
	self.data[element] = nil
end

function Set:ContainsPartially(element)
	for key, _ in pairs(self.data) do
		if string.find(ToLower(key), ToLower(element)) then
			return true
        end
    end
    return false
end

function Set:Contains(element)
	return self.data[element] ~= nil
end

function Set:ContainsCi(element)
	for key, _ in pairs(self.data) do
		if ToLower(key) == ToLower(element) then
			return true
        end
    end
    return false
end

function ToLower(str)
	return string.lower(str)
end

return Set