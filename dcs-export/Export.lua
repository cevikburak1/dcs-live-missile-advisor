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

local WH_TARGET_RADAR_LOCK = 0x0008
local WH_TARGET_RADAR_TRACK = 0x0020
local WH_TARGET_EOS_LOCK = 0x0010
local WH_TARGET_EOS_TRACK = 0x0040
local WH_TARGET_LOCK_ON_JAMMER = 0x0800

local function band(a, b)
  if bit then return bit.band(a, b) end
  if bit32 then return bit32.band(a, b) end
  return 0
end

local function safe_call(fn, ...)
  if not fn then return nil end
  local ok, result = pcall(fn, ...)
  if ok then return result end
  return nil
end

local function safe_number(v)
  if v == nil or type(v) ~= "number" then return nil end
  if v ~= v then return nil end
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
  if not entry then return nil end
  local t = entry.target or entry
  if not t then return nil end

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
  if not raw then return {} end
  local out = {}
  if type(raw) == "table" then
    for _, entry in pairs(raw) do
      local norm = normalize_target_entry(entry)
      if norm and (norm.distance_m ~= nil or norm.id ~= nil) then
        out[#out + 1] = norm
      end
    end
  end
  return out
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
          container = st.container
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
        heading_rad = safe_number(self_data.Heading)
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
    packet.targets_locked = collect_targets(LoGetLockedTargetInformation)
    packet.targets_info = collect_targets(LoGetTargetInformation)
    packet.tws = build_tws_info()
  end

  return packet
end

local function send_packet()
  if not udp or not json then return end
  local ok, packet = pcall(build_packet)
  if not ok or not packet then return end
  local msg = json.encode(packet)
  pcall(function() udp:send(msg) end)
end

function LuaExportStart()
  package.path = package.path .. ";.\\LuaSocket\\?.lua"
  package.cpath = package.cpath .. ";.\\LuaSocket\\?.dll"

  local script_dir = lfs.writedir() .. "Scripts\\DcsMissileAdvisor\\"
  package.path = package.path .. ";" .. script_dir .. "?.lua"

  local ok, sock = pcall(require, "socket")
  if not ok then return end
  socket = sock

  local ok_json, j = pcall(require, "json")
  if ok_json then json = j end

  udp = socket.udp()
  udp:settimeout(0)
  udp:setpeername(UDP_HOST, UDP_PORT)
end

function LuaExportStop()
  if udp then
    pcall(function() udp:close() end)
    udp = nil
  end
end

function LuaExportAfterNextFrame()
  -- unused; rate limited via ActivityNextEvent
end

function LuaExportActivityNextEvent(t)
  send_packet()
  return t + TICK_INTERVAL
end
