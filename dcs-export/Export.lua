-- DCS Live Universal Missile Advisor - Export Script
-- Install: Saved Games/DCS/Scripts/DcsMissileAdvisor/Export.lua
-- Hook:  dofile(lfs.writedir() .. "Scripts/DcsMissileAdvisor/Export.lua")

local UDP_HOST = "127.0.0.1"
local UDP_PORT = 17777
local TICK_INTERVAL = 0.125  -- ~8 Hz

local socket = nil
local udp = nil
local seq = 0
local json = nil
local hornet = nil
local previous = {
  start = LuaExportStart, stop = LuaExportStop,
  frame = LuaExportAfterNextFrame, activity = LuaExportActivityNextEvent
}
local next_send = 0
local previous_next = 0
local logged_errors = {}

local function report(message)
  if log and log.write then log.write("DcsMissileAdvisor", log.INFO, message) end
end

local function report_once(message)
  if not logged_errors[message] then
    logged_errors[message] = true
    report(message)
  end
end

local WH_TARGET_RADAR_LOCK = 0x0008
local WH_TARGET_RADAR_TRACK = 0x0020
local WH_TARGET_EOS_LOCK = 0x0010
local WH_TARGET_EOS_TRACK = 0x0040
local WH_TARGET_LOCK_ON_JAMMER = 0x0800

local function band(a, b)
  if bit then return bit.band(a, b) end
  if bit32 then return bit32.band(a, b) end
  local result, place = 0, 1
  while a > 0 and b > 0 do
    if a % 2 == 1 and b % 2 == 1 then result = result + place end
    a, b, place = math.floor(a / 2), math.floor(b / 2), place * 2
  end
  return result
end

local function safe_call(fn, ...)
  if not fn then return nil end
  local ok, result = pcall(fn, ...)
  if ok then return result end
  return nil
end

local function safe_number(v)
  if v == nil or type(v) ~= "number" then return nil end
  if v ~= v or v == math.huge or v == -math.huge then return nil end
  return v
end

local function vec3(tbl)
  if not tbl then return nil end
  return {
    x = safe_number(tbl.x),
    y = safe_number(tbl.y),
    z = safe_number(tbl.z)
  }
end

local function type_tuple(t)
  if not t then return nil end
  if t.level1 then
    return {
      safe_number(t.level1),
      safe_number(t.level2),
      safe_number(t.level3),
      safe_number(t.level4)
    }
  end
  if type(t) == "table" and #t >= 4 then
    return { safe_number(t[1]), safe_number(t[2]), safe_number(t[3]), safe_number(t[4]) }
  end
  return nil
end

local function normalize_target_entry(entry)
  if type(entry) ~= "table" then return nil end
  local t = entry.target or entry
  if type(t) ~= "table" then return nil end

  local pos_y = nil
  if t.position and t.position.p then
    pos_y = safe_number(t.position.p.y)
  end

  local alt_msl = nil
  local id = t.ID or t.id
  if id and id ~= 0 and LoIsObjectExportAllowed and LoIsObjectExportAllowed() then
    local obj = safe_call(LoGetObjectById, id)
    if obj and obj.LatLongAlt then
      alt_msl = safe_number(obj.LatLongAlt.Alt)
    end
  end

  local flags = safe_number(t.flags)
  local is_jamming = t.isjamming
  if is_jamming == nil and flags then
    is_jamming = band(flags, WH_TARGET_LOCK_ON_JAMMER) ~= 0
  end

  return {
    id = id,
    distance_m = safe_number(t.distance),
    convergence_mps = safe_number(t.convergence_velocity),
    delta_psi_rad = safe_number(t.delta_psi),
    course_rad = safe_number(t.course),
    is_jamming = is_jamming,
    flags = flags,
    pos_y_m = pos_y,
    alt_msl_m = alt_msl,
    mach = safe_number(t.mach)
  }
end

local function collect_targets(fn)
  local raw = safe_call(fn)
  if type(raw) ~= "table" then return {}, false end
  local out = {}
  if raw.ID or raw.id or raw.target or raw.distance then raw = { raw } end
  for _, entry in pairs(raw) do
      local norm = normalize_target_entry(entry)
      if norm and (norm.distance_m ~= nil or norm.id ~= nil) then
        out[#out + 1] = norm
      end
  end
  return out, true
end

local function build_payload_info()
  local p = safe_call(LoGetPayloadInfo)
  if not p then return nil end

  local stations = {}
  if p.Stations then
    for i, st in pairs(p.Stations) do
      if st and st.weapon then
        local wt = type_tuple(st.weapon)
        local name = nil
        if wt and wt[1] and LoGetNameByType then
          name = safe_call(LoGetNameByType, wt[1], wt[2], wt[3], wt[4])
        end
        stations[#stations + 1] = {
          idx = i,
          count = safe_number(st.count),
          type = wt,
          name = name,
          container = type(st.container) == "boolean" and st.container or nil
        }
      end
    end
  end

  local cannon_shells = nil
  if p.Cannon and p.Cannon.shells then
    cannon_shells = safe_number(p.Cannon.shells)
  end

  return {
    current_station = safe_number(p.CurrentStation),
    stations = stations,
    cannon_shells = cannon_shells
  }
end

local function build_tws_info()
  local tws = safe_call(LoGetTWSInfo)
  if not tws then return nil end

  local emitters = {}
  if tws.Emitters then
    for _, e in pairs(tws.Emitters) do
      if e then
        emitters[#emitters + 1] = {
          id = e.ID,
          azimuth = safe_number(e.Azimuth),
          signal = e.SignalType,
          priority = safe_number(e.Priority)
        }
      end
    end
  end

  return {
    mode = safe_number(tws.Mode),
    emitters = emitters
  }
end

local function build_packet()
  seq = seq + 1

  local perm_ownship = LoIsOwnshipExportAllowed and LoIsOwnshipExportAllowed() or false
  local perm_sensor = LoIsSensorExportAllowed and LoIsSensorExportAllowed() or false
  local perm_object = LoIsObjectExportAllowed and LoIsObjectExportAllowed() or false

  local packet = {
    v = 1,
    seq = seq,
    t = safe_number(LoGetModelTime and LoGetModelTime()),
    perm = {
      ownship = perm_ownship,
      sensor = perm_sensor,
      object = perm_object
    },
    self = nil,
    flight = nil,
    payload = nil,
    targets_locked = {},
    targets_info = {},
    tws = nil
  }

  if perm_ownship then
    local self_data = safe_call(LoGetSelfData)
    if self_data then
      packet.self = {
        name = self_data.Name,
        unit_name = self_data.UnitName,
        type = type_tuple(self_data.Type),
        heading_rad = safe_number(self_data.Heading),
        alt_msl_m = self_data.LatLongAlt and safe_number(self_data.LatLongAlt.Alt)
      }
    end

    packet.flight = {
      mach = safe_number(safe_call(LoGetMachNumber)),
      ias_mps = safe_number(safe_call(LoGetIndicatedAirSpeed)),
      tas_mps = safe_number(safe_call(LoGetTrueAirSpeed)),
      alt_msl_m = safe_number(safe_call(LoGetAltitudeAboveSeaLevel)),
      alt_agl_m = safe_number(safe_call(LoGetAltitudeAboveGroundLevel)),
      vel = vec3(safe_call(LoGetVectorVelocity))
    }

    packet.payload = build_payload_info()
  end

  if perm_sensor then
    packet.targets_locked, packet.target_api_available = collect_targets(LoGetLockedTargetInformation)
    packet.targets_info = collect_targets(LoGetTargetInformation)
    packet.tws = build_tws_info()
  end

  if perm_ownship and hornet and packet.self and packet.self.name == "FA-18C_hornet" then
    local cockpit = hornet.collect(function(id) return safe_call(list_indication, id) end,
      packet.payload, perm_sensor)
    packet.weapon_selected_name = cockpit.weapon_selected_name
    packet.cockpit_target = cockpit.target
    packet.cockpit_diagnostics = cockpit.diagnostics
  end
  packet.export_version = "1.1"

  return packet
end

local function send_packet()
  if not udp or not json then return end
  local ok, packet = pcall(build_packet)
  if not ok then report_once("Packet collection failed: " .. tostring(packet)); return end
  local encoded, msg = pcall(json.encode, packet)
  if not encoded then report_once("JSON encoding failed: " .. tostring(msg)); return end
  local sent, err = udp:send(msg)
  if not sent then report_once("UDP send failed: " .. tostring(err)) end
end

function LuaExportStart()
  safe_call(previous.start)
  seq, next_send, previous_next, logged_errors = 0, 0, 0, {}
  package.path = package.path .. ";.\\LuaSocket\\?.lua"
  package.cpath = package.cpath .. ";.\\LuaSocket\\?.dll"

  local script_dir = lfs.writedir() .. "Scripts\\DcsMissileAdvisor\\"
  package.path = package.path .. ";" .. script_dir .. "?.lua"

  local ok, sock = pcall(require, "socket")
  if not ok then report("LuaSocket load failed: " .. tostring(sock)); return end
  socket = sock

  local ok_json, j = pcall(dofile, script_dir .. "json.lua")
  if not ok_json then report("JSON load failed: " .. tostring(j)); return end
  json = j
  local ok_hornet, h = pcall(dofile, script_dir .. "Hornet.lua")
  if ok_hornet then hornet = h else report("Hornet adapter load failed: " .. tostring(h)) end

  udp = socket.udp()
  udp:settimeout(0)
  udp:setpeername(UDP_HOST, UDP_PORT)
  report("Export 1.1 started: " .. UDP_HOST .. ":" .. UDP_PORT)
end

function LuaExportStop()
  if udp then
    pcall(function() udp:close() end)
    udp = nil
  end
  safe_call(previous.stop)
end

function LuaExportAfterNextFrame()
  safe_call(previous.frame)
end

function LuaExportActivityNextEvent(t)
  if t >= next_send then
    send_packet()
    next_send = t + TICK_INTERVAL
  end
  if previous.activity and previous_next and t >= previous_next then
    local value = safe_call(previous.activity, t)
    previous_next = type(value) == "number" and value > t and value or nil
  end
  return previous_next and math.min(next_send, previous_next) or next_send
end
