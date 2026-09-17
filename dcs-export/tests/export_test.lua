-- Run from the repository root with a Lua 5.1 interpreter. No network or DCS required.
local json = dofile("dcs-export/json.lua")
local hornet = dofile("dcs-export/Hornet.lua")
local checks = 0
local function check(value, message)
  assert(value, message)
  checks = checks + 1
end
local function indication(fields)
  local lines = {}
  for key, value in pairs(fields) do
    lines[#lines + 1] = "-----------------------------------------\n" .. key .. "\n" .. value .. "\n"
  end
  return table.concat(lines)
end
local payload = { current_station = 0, stations = {
  { idx = 4, count = 1, name = "AIM_7P" }, { idx = 6, count = 1, name = "AIM_7P" },
  { idx = 2, count = 2, name = "AIM_9X" }
} }
local hud = { AA_Weapon_type = "SP", HUD_AA_targetRange_FLOOD = "12.5RNG", HUD_AA_targetRangeRate = "450V" }
local ddi = { Current_AA_Weapon = "SP", ASPECT_DDI = "18", TUC_PlaceholderMainTrack_Mach = "0.85",
  TUC_PlaceholderMainTrack_Altitude = "25", TUC_PlaceholderTUC_Altitude = "40" }
local function read(id) return indication(id == 1 and hud or ddi) end
local cockpit = hornet.collect(read, payload, true)
check(cockpit.weapon_selected_name == "AIM_7P", "SP selects the sole loaded Sparrow variant")
check(cockpit.target.distance_m == 23150, "HUD range is nautical miles")
check(math.abs(cockpit.target.convergence_mps - 231.5) < 0.001, "HUD closure is knots")
check(cockpit.target.alt_msl_m == 7620, "main-track altitude, not cursor altitude")
check(cockpit.target.delta_psi_rad == nil, "launch aspect cue is not actual target aspect")
check(cockpit.target.flags == nil, "HUD track does not establish STT")
hud.HUD_AA_targetRange_FLOOD = "3000 FT "
check(math.abs(hornet.collect(read, payload, true).target.distance_m - 914.4) < 0.001, "close HUD range uses feet")
hud.HUD_AA_targetRange_FLOOD = "FLOOD"
check(hornet.collect(read, payload, true).target == nil, "FLOOD is not a ranged target")
hud.HUD_AA_targetRange_FLOOD = "12.5RNG"
hud.MEM_RMEM_Label = "MEM"
check(hornet.collect(read, payload, true).target == nil, "memory track is not a live lock")
hud.MEM_RMEM_Label = nil
check(hornet.collect(read, payload, false).target == nil, "sensor restrictions respected")
payload.stations[#payload.stations + 1] = { idx = 7, count = 1, name = "AIM_7M" }
check(hornet.collect(read, payload, true).weapon_selected_name == nil, "mixed Sparrow variants are ambiguous")
hud.AA_Weapon_type = "7P"
check(hornet.collect(read, payload, true).weapon_selected_name == "AIM-7P", "explicit variant is usable")
hud = { stores_label = "AIM-7P", HUD_RangeAndIdentification = "12.0 W1" }
check(hornet.collect(read, payload, true).weapon_selected_name == nil, "stores label is not selection")
check(hornet.collect(read, payload, true).target == nil, "waypoint range is not target range")

-- Execute the actual exporter and serialize its packet as the backend contract fixture.
local sent, previous_start, previous_frame, previous_activity, previous_stop = nil, 0, 0, 0, 0
LuaExportStart = function() previous_start = previous_start + 1 end
LuaExportAfterNextFrame = function() previous_frame = previous_frame + 1 end
LuaExportStop = function() previous_stop = previous_stop + 1 end
LuaExportActivityNextEvent = function(t) previous_activity = previous_activity + 1; return t + 0.5 end
lfs = { writedir = function() return "test/" end }
package.preload.socket = function() return { udp = function() return {
  settimeout = function() end, setpeername = function() end, close = function() end,
  send = function(_, text) sent = text; return #text end
} end } end
local original_dofile = dofile
dofile = function(path)
  if path:match("json.lua$") then return json end
  if path:match("Hornet.lua$") then return hornet end
  return original_dofile(path)
end
LoIsOwnshipExportAllowed = function() return true end
LoIsSensorExportAllowed = function() return true end
LoIsObjectExportAllowed = function() return false end
LoGetSelfData = function() return { Name = "FA-18C_hornet", UnitName = "Test", Heading = math.pi / 2,
  LatLongAlt = { Alt = 5000 } } end
LoGetModelTime = function() return 42 end
LoGetMachNumber = function() return 0.85 end
LoGetIndicatedAirSpeed = function() return 200 end
LoGetTrueAirSpeed = function() return 250 end
LoGetAltitudeAboveSeaLevel = function() return 5000 end
LoGetAltitudeAboveGroundLevel = function() return 4500 end
LoGetVectorVelocity = function() return { x = 200, y = 0, z = 150 } end
LoGetPayloadInfo = function() return { CurrentStation = 4, Cannon = { shells = 578 }, Stations = {
  [4] = { count = 1, weapon = { level1 = 4, level2 = 4, level3 = 7, level4 = 1 } }
} } end
LoGetNameByType = function() return "AIM_7P" end
LoGetLockedTargetInformation = function() return { { ID = 16777217, distance = 18520, convergence_velocity = 150,
  delta_psi = 0.1, course = 2.1, mach = 0.8, flags = 8, position = { p = { y = 6000 } } } } end
LoGetTargetInformation = function() return {} end
original_dofile("dcs-export/Export.lua")
LuaExportStart()
LuaExportAfterNextFrame()
LuaExportActivityNextEvent(0)
check(sent and sent:find('"ias_mps":200', 1, true), "real exporter emits snake_case units")
check(previous_start == 1 and previous_frame == 1 and previous_activity == 1, "other exporters remain chained")
local fixture = assert(io.open(".artifacts/export-fixture.json", "w"))
fixture:write(sent)
fixture:close()
LuaExportActivityNextEvent(0.125)
check(previous_activity == 1, "other exporter schedule preserved")
LoGetLockedTargetInformation = function() return { ID = 456, distance = 1000, flags = 2048 } end
LuaExportActivityNextEvent(0.25)
check(sent:find('"id":456', 1, true), "single target table supported")
check(sent:find('"is_jamming":true', 1, true), "flags work without Lua bit libraries")
LoIsOwnshipExportAllowed = function() return false end
LoIsSensorExportAllowed = function() return false end
LuaExportActivityNextEvent(0.5)
check(not sent:find('"flight"', 1, true) and not sent:find('"cockpit_target"', 1, true), "denied data omitted")
LuaExportStop()
check(previous_stop == 1, "other exporter stop preserved")
print("PASS: " .. checks .. " Lua export checks")
