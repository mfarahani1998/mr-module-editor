# MVP Spatial Tuning

## Convention

Anchor pose is the base attachment plane in world space.
Layout.position is the local offset from that anchor plane.
Panel child z offsets are tiny render-depth values only, not physical distance.

## Starting values

| Element | Anchor | Distance source | Layout offset | Starting size |
|---|---|---|---|---|
| Text instruction | head | AnchorResolver.headDistance | (0, -0.45, 0) | width about 1.4–1.8m |
| Image panel | head | AnchorResolver.headDistance | (0, -0.20, 0) | width about 1.3–1.6m |
| MCQ panel | head | AnchorResolver.headDistance | (0, -0.15, 0) | width about 1.5–1.8m |
| Robot object | world | anchor.world.table | module-specific | scene object scale |
| Object callout | object | object position | (0, 0.5–1.2, 0) | scale 0.5–0.8 |

## Tuning order

Tune only one variable at a time:

1. headDistance
2. panel scale or panel size
3. y offset
4. text size and wrap
5. only then z offset, if intentionally needed