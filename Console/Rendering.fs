module Rendering
open OpenTK
open primitives

type StaticRenderContext = {
        ViewMatrix : Matrix4
        ProjectionMatrix : Matrix4
    }

type IndividualRenderContext = {
        ModelMatrix : Matrix4
        NormalMatrix : Matrix3 //To view space
    }

type IndividualRenderJob = {
        IndividualContext : IndividualRenderContext
        Mesh : meshWithNormals
    }

type RenderJob = {
        StaticContext : StaticRenderContext
        RenderJobs : list<IndividualRenderJob>
    }

