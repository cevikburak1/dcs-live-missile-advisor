-- Minimal JSON encoder for DCS Export.lua (no decode needed)
local json = {}

local escape_map = {
  ["\\"] = "\\\\",
  ["\""] = "\\\"",
  ["\n"] = "\\n",
  ["\r"] = "\\r",
  ["\t"] = "\\t",
  ["\b"] = "\\b",
  ["\f"] = "\\f",
}

local function escape_str(s)
  return (s:gsub('[\\"\n\r\t\b\f]', escape_map))
end

function json.null()
  return "null"
end

function json.encode(val)
  if val == nil then
    return "null"
  end
  local t = type(val)
  if t == "boolean" then
    return val and "true" or "false"
  elseif t == "number" then
    if val ~= val or val == math.huge or val == -math.huge then
      return "null"
    end
    -- DCS object IDs must stay exact integers; exponent notation cannot decode as Int64.
    if val % 1 == 0 then return string.format("%.0f", val) end
    return string.format("%.17g", val)
  elseif t == "string" then
    return '"' .. escape_str(val) .. '"'
  elseif t == "table" then
    if #val > 0 or next(val) == nil then
      local parts = {}
      for i = 1, #val do
        parts[#parts + 1] = json.encode(val[i])
      end
      return "[" .. table.concat(parts, ",") .. "]"
    else
      local parts = {}
      for k, v in pairs(val) do
        if type(k) == "string" then
          parts[#parts + 1] = '"' .. escape_str(k) .. '":' .. json.encode(v)
        end
      end
      return "{" .. table.concat(parts, ",") .. "}"
    end
  end
  return "null"
end

return json
