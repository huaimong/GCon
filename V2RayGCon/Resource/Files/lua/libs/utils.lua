local u={}

function u.Echo(message)
	print(message)
end

function u.WriteLine(filename, content)
	local file=io.open(filename, "a")
    file:write(content .. "\n")
    file:close()
end

function u.IsInList(needle, haystack)
	for _, item in ipairs(haystack)
    do
        if string.lower(needle) == string.lower(item) then
            return true
        end
    end
    return false
end

return u