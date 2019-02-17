-- import libs and models
local Utils = require "lua.libs.utils"
local Logger = require "lua.models.logger"
local Set = require "lua.models.set"

function Main()
    UtilsExample()
    LoggerExample()
    SetExample()
end

function UtilsExample()
    -- Example 1. how-to use libs.utils.lua
    Utils.Echo("Hello")
end

function LoggerExample()
    -- Example 2. how-to use class
    local logger = Logger("logExample.txt")
    logger:Info("Hello")
    logger:Warn("world")
    logger:Error("!")
    logger:Log("nothing", "here")
end

function SetExample()
    -- Example 3. how-to use set
    local cache = Set()

    cache:Add(1)
    print(cache:Contains(1))

    cache:Add(1)
    cache:Add(2)
    cache:Remove(1)
    print(cache:Contains(1))
    print(cache:Contains(2))
end

Main()