using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Rule Tile que trata a sus "hermanos" como si fueran el mismo tile.
// Así tierra, mezcla y muro se unen sin dibujar bordes entre ellos.
// Basado en el ejemplo "Siblings" del manual de 2D Tilemap Extras (Custom Rules for Rule Tile).
public class RuleTileHermanos : RuleTile
{
    // Lista que aparece en el Inspector: aquí van los otros tiles que cuentan como "iguales".
    public List<TileBase> hermanos = new List<TileBase>();

    // Unity llama a esta función por cada vecino: "¿este vecino es igual a mí?"
    public override bool RuleMatch(int neighbor, TileBase other)
    {
        bool igual = other == this || hermanos.Contains(other);
        if (neighbor == TilingRuleOutput.Neighbor.This) return igual;
        if (neighbor == TilingRuleOutput.Neighbor.NotThis) return !igual;
        return base.RuleMatch(neighbor, other);
    }
}
