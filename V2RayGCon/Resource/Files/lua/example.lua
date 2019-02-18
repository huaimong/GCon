local Writer = require "lua.models.writer"
local Logger = require "lua.models.logger"

local testDataFileName = "testData.txt"
local testLogFileName = "testLog.txt"

function WriterExample()
    local dw = Writer(testDataFileName)
    print("Write hello to file.")
    dw:WriteLine("hello")
    print("Clear file.")
    dw:Clear()
    print("Write hello world to file.")
    dw:WriteLine("Hello, world!")
end

function LoggerExample()
    print("Run logger tests")
    local logger = Logger(testLogFileName)
    logger:Info("Hello")
    logger:Warn("world")
    logger:Error("!")
    logger:Log("nothing", "here")
end

function Main()
   WriterExample() 
   LoggerExample()
end

Main()