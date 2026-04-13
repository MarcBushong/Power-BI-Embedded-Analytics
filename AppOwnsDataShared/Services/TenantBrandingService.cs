using System;
using System.Collections.Generic;
using AppOwnsDataShared.Models;

namespace AppOwnsDataShared.Services {

  public class TenantBranding {
    public string ThemePrimary   { get; set; }
    public string ThemeSecondary { get; set; }
    public string ThemeTertiary  { get; set; }
    public string LogoSymbol     { get; set; }
    public string Tagline        { get; set; }
  }

  public static class TenantBrandingService {

    // ── Known tenant templates ──────────────────────────────────────────────
    // These persist in code so deleting + re-onboarding a tenant always
    // restores the same branding automatically.
    private static readonly Dictionary<string, TenantBranding> Templates =
      new Dictionary<string, TenantBranding>(StringComparer.OrdinalIgnoreCase) {

        ["WingTip"] = new TenantBranding {
          ThemePrimary   = "#0D1B4B",
          ThemeSecondary = "#1565C0",
          ThemeTertiary  = "#00B0FF",
          LogoSymbol     = "WT",
          Tagline        = "Soaring beyond expectations"
        },

        ["Contoso"] = new TenantBranding {
          ThemePrimary   = "#880E4F",
          ThemeSecondary = "#C2185B",
          ThemeTertiary  = "#FFB300",
          LogoSymbol     = "CO",
          Tagline        = "Innovation meets elegance"
        },

        ["MegaCorp"] = new TenantBranding {
          ThemePrimary   = "#1A237E",
          ThemeSecondary = "#283593",
          ThemeTertiary  = "#FF6F00",
          LogoSymbol     = "MC",
          Tagline        = "Enterprise solutions at scale"
        },

        ["AcmeCorp"] = new TenantBranding {
          ThemePrimary   = "#004D40",
          ThemeSecondary = "#00695C",
          ThemeTertiary  = "#FFD600",
          LogoSymbol     = "AC",
          Tagline        = "Building tomorrow, today"
        },
      };

    // ── Curated palettes for dynamically generated tenants ─────────────────
    // Chosen so any two adjacent entries in the list look distinct.
    private static readonly (string primary, string secondary, string tertiary)[] Palettes = {
      ("#7B1FA2", "#9C27B0", "#E040FB"),  // violet
      ("#C62828", "#E53935", "#FF8A65"),  // coral-red
      ("#0277BD", "#0288D1", "#4FC3F7"),  // sky-blue
      ("#2E7D32", "#388E3C", "#69F0AE"),  // emerald
      ("#E65100", "#F57C00", "#FFB74D"),  // orange
      ("#006064", "#00838F", "#26C6DA"),  // cyan-teal
      ("#37474F", "#455A64", "#80DEEA"),  // blue-slate
      ("#AD1457", "#C2185B", "#F48FB1"),  // rose
    };

    // ── Public API ──────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the branding for a tenant.
    /// Exact template match → partial match → deterministic generated palette.
    /// </summary>
    public static TenantBranding GetBranding(string tenantName) {
      if (string.IsNullOrWhiteSpace(tenantName))
        return BuildGenerated("T", tenantName ?? "");

      // exact match (case-insensitive)
      if (Templates.TryGetValue(tenantName, out var exact))
        return exact;

      // partial match (e.g. "WingTip01" → WingTip template)
      foreach (var kv in Templates) {
        if (tenantName.IndexOf(kv.Key, StringComparison.OrdinalIgnoreCase) >= 0)
          return kv.Value;
      }

      // deterministic generated branding for unknown tenants
      return BuildGenerated(
        sym: tenantName.Length >= 2
               ? tenantName.Substring(0, 2).ToUpperInvariant()
               : tenantName.ToUpperInvariant(),
        tenantName: tenantName);
    }

    /// <summary>
    /// Writes branding fields onto a PowerBiTenant instance in-place.
    /// Called automatically from AppOwnsDataDBService.OnboardNewTenant().
    /// </summary>
    public static void ApplyBrandingToTenant(PowerBiTenant tenant) {
      var b = GetBranding(tenant.Name);
      tenant.ThemePrimary   = b.ThemePrimary;
      tenant.ThemeSecondary = b.ThemeSecondary;
      tenant.ThemeTertiary  = b.ThemeTertiary;
      tenant.LogoSymbol     = b.LogoSymbol;
      tenant.Tagline        = b.Tagline;
    }

    // ── Private helpers ─────────────────────────────────────────────────────

    private static TenantBranding BuildGenerated(string sym, string tenantName) {
      // Deterministic palette: same tenant name always → same colors
      var hash      = Math.Abs(GetStableHash(tenantName));
      var palette   = Palettes[hash % Palettes.Length];
      return new TenantBranding {
        ThemePrimary   = palette.primary,
        ThemeSecondary = palette.secondary,
        ThemeTertiary  = palette.tertiary,
        LogoSymbol     = sym,
        Tagline        = tenantName + " Analytics"
      };
    }

    // GetHashCode is not stable across runs in .NET Core; use a manual hash.
    private static int GetStableHash(string s) {
      unchecked {
        int h = 17;
        foreach (char c in s) h = h * 31 + c;
        return h;
      }
    }
  }
}
