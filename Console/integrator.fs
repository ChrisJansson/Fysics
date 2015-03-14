module integrator
    open vector
    open particle

    let zero = { x = 0.0; y = 0.0; z = 0.0 }

    let integrate dt p =
        let nextPosition = p.position + dt * p.velocity
        let nextVelocity = dt * p.acceleration + p.velocity
        let dampedVelocity = p.damping ** dt * nextVelocity
        { p with position = nextPosition; velocity = dampedVelocity; forceAccumulator = zero };

    let integrateAll dt particles =
        particles
        |> Seq.map (integrate dt)
