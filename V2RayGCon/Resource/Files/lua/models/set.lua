assert(Lcs, "Please add 'Lcs = require \"lua.models.lcs\"' at the top of this script")

function ToLower(str)
	return string.lower(str)
end

local Set = Lcs.class()

function Set:init(initData)
	self.datas = {}
	if initData ~= nil and type(initData) == "table" then
		for _, item in pairs(initData) do
			self.datas[item] = true
		end
	end	
end

function Set:Count()
	local count = 0
	for key, item in pairs(self.datas) do	
		-- print("data ", key, " = ", item)
		if item == true then
			count = count + 1
		end
	end
	return count
end

function Set:Reset(data)
	self.datas = {}
	if data == nil then
		return
	end
	for key, item in pairs(data) do
		-- print("set ", key, " = ", item)
		self.datas[item] = true
	end
end

function Set:Add(element)
	self.datas[element] = true
end

function Set:Remove(element)
	self.datas[element] = nil
end

function Set:MatchesPartially(text)
	for key, _ in pairs(self.datas) do
		if string.find(ToLower(text), ToLower(key)) then
			return true
		end
	end
	return false
end

function Set:ContainsPartially(element)
	for key, _ in pairs(self.datas) do
		if string.find(ToLower(key), ToLower(element)) then
			return true
        end
    end
    return false
end

function Set:Contains(element)
	return self.datas[element] ~= nil
end

function Set:ContainsCi(element)
	for key, _ in pairs(self.datas) do
		if ToLower(key) == ToLower(element) then
			return true
        end
    end
    return false
end

return Set