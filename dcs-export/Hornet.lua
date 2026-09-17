-- Read named, visible A/A indications only; never infer a selection from stores text.
local Hornet = {}

local function trim(value)
  return (value or ""):match("^%s*(.-)%s*$")
end

function Hornet.parse(text)
  local fields = {}
  if type(text) ~= "string" then return fields end
  text = text:gsub("\r", "") .. "\n"
  for key, value in text:gmatch("%-+\n([^\n]+)\n([^\n]*)\n") do
    key, value = trim(key), trim(value)
    if fields[key] and fields[key] ~= value then
      fields[key] = false -- repeated track names are ambiguous
    elseif fields[key] == nil then
      fields[key] = value
    end
  end
  return fields
end

local function number(value)
  if type(value) ~= "string" then return nil end
  return tonumber(trim(value))
end

local function selected_weapon(label, payload)
  if type(label) ~= "string" then return nil end
  label = trim(label):upper()
  local names = {
    ["7P"] = "AIM-7P", ["7M"] = "AIM-7M", ["7F"] = "AIM-7F",
    ["9X"] = "AIM-9X", ["9M"] = "AIM-9M",
    ["120C"] = "AIM-120C", ["120B"] = "AIM-120B"
  }
  if names[label] then return names[label] end
  local family = ({ SP = "AIM7", AM = "AIM120", SW = "AIM9", ["9"] = "AIM9" })[label]
  if not family then return nil end
  local candidate, candidate_key
  for _, station in ipairs(payload and payload.stations or {}) do
    if station.name and (station.count or 0) > 0 then
      local key = station.name:upper():gsub("[%s_%-]", "")
      if key:sub(1, #family) == family then
        if candidate_key and candidate_key ~= key then return nil end
        candidate, candidate_key = station.name, key
      end
    end
  end
  return candidate
end

function Hornet.collect(read, payload, sensor_allowed)
  local hud = Hornet.parse(read(1))
  local result = {
    weapon_selected_name = selected_weapon(hud.AA_Weapon_type, payload),
    diagnostics = { weapon_label = hud.AA_Weapon_type or nil }
  }
  -- Weapon labels are ownship data; sensor fields are never read with export denied.
  if not sensor_allowed then return result end
  local range_text = trim(hud.HUD_AA_targetRange_FLOOD or "")
  local range_nm = tonumber(range_text:match("^(%d+%.?%d*)%s*RNG$"))
  local range_ft = tonumber(range_text:match("^(%d+%.?%d*)%s*FT$"))
  local distance = range_nm and range_nm * 1852 or range_ft and range_ft * 0.3048
  local closure = tonumber(trim(hud.HUD_AA_targetRangeRate or ""):match("^([%+%-]?%d+%.?%d*)%s*V[C]?$"))
  result.diagnostics.range_text = range_text
  result.diagnostics.closure_text = hud.HUD_AA_targetRangeRate or nil
  result.diagnostics.memory = hud.MEM_RMEM_Label or nil
  if not distance or distance <= 0 or closure == nil then return result end
  if trim(hud.MEM_RMEM_Label or "") ~= "" then return result end
  if trim(hud.HUD_AA_targetRangeUncertainty or "") ~= "" then return result end
  if trim(hud.HUD_AA_TD_box_NO_RDR_cue or "") ~= "" then return result end

  local target = {
    distance_m = distance,
    convergence_mps = closure * 1852 / 3600,
    source = "Hornet HUD/DDI"
  }
  -- Use a single radar display's main-track data, never targets under the cursor.
  for _, id in ipairs({ 2, 3, 4 }) do
    local display = Hornet.parse(read(id))
    if display.Current_AA_Weapon then
      result.weapon_selected_name = result.weapon_selected_name
        or selected_weapon(display.Current_AA_Weapon, payload)
      target.mach = number(display.TUC_PlaceholderMainTrack_Mach)
        or number(display.MainTrack_mach_altitude_PlaceholderMainTrack_Mach)
      local altitude = number(display.TUC_PlaceholderMainTrack_Altitude)
        or number(display.MainTrack_mach_altitude_PlaceholderMainTrack_Altitude)
      target.alt_msl_m = altitude and altitude * 1000 * 0.3048
      if display.MAIN_TRACK_Jamming == "J" then target.is_jamming = true end
      -- ASPECT_DDI is the allowable launch aspect cue, not measured target aspect.
      break
    end
  end
  result.target = target
  return result
end

return Hornet
