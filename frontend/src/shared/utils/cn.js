export function cn(...values) {
  const out = [];

  for (const value of values) {
    if (!value) continue;

    if (typeof value === "string") out.push(value);
    else if (Array.isArray(value)) {
      const nested = cn(...value);
      if (nested) out.push(nested);
    } else if (typeof value === "object") {
      for (const [key, active] of Object.entries(value))
        if (active) out.push(key);
    }
  }

  return out.join(" ");
}
