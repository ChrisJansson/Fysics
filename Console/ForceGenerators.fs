module ForceGenerators
open vector
open particle

type ForceGenerator = particle -> float -> vector3

let DragForceGenerator k1 k2 p dt =
    let m = magnitude p.velocity
    let dragMagnitude = k1 * m + k2 * m * m
    -dragMagnitude * (normalize p.velocity)