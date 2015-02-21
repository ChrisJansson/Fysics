module integrator
    open vector
    open particle

    let integrate dt p =
        let nextPosition = p.position + dt * p.velocity
        let nextVelocity = dt * p.acceleration + p.velocity
        let dampedVelocity = p.damping ** dt * nextVelocity
        { p with position = nextPosition; velocity = dampedVelocity };

    let integrateAll dt particles =
        particles
        |> Seq.map (integrate dt)
