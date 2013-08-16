HEX_SIZE = 1.0

function axialToCartesian(q, r)
    local x = q * HEX_SIZE * 3 / 2
    local y = (r + q/2) * HEX_SIZE * math.sqrt(3)
    return x, y
end

