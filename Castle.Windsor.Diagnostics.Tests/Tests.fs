﻿module Tests

open MbUnit.Framework
open Castle.Core
open Castle.MicroKernel
open Castle.MicroKernel.Handlers
open Castle.MicroKernel.Registration
open Castle.Windsor
open QuickGraph

type IComp = interface end
type CompA(c: IComp) = interface IComp
type CompB(c: IComp) = interface IComp
type CompC(c: IComp) = interface IComp

[<Test>]
let cycle() =
    use c = new WindsorContainer()
    let reg = [|
                Component.For<IComp>().ImplementedBy<CompA>().Named("a")
                Component.For<IComp>().ImplementedBy<CompB>().Named("b")
                Component.For<IComp>().ImplementedBy<CompC>().Named("c")
              |] |> Array.cast
    c.Register(reg) |> ignore
    assertThrows<HandlerException>(fun() -> c.Resolve<IComp>("a") |> ignore)

let loadGraphFromKernel (k: IKernel) = 
    let handlers = k.GetAssignableHandlers(typeof<obj>)
    let descType o = o.GetType().AssemblyQualifiedName
    let descLS (t: LifestyleType) = 
        let t = match t with 
                | LifestyleType.Undefined -> LifestyleType.Singleton 
                | _ -> t
        t.ToString()
    let describe (m: ComponentModel) = sprintf "%s;%s;%s;%s" m.Name (descType m.Service) (descType m.Implementation) (descLS m.LifestyleType)
    let describeHandler (h: IHandler) = describe h.ComponentModel
    let dependencyDict = handlers 
                         |> Seq.distinctBy describeHandler 
                         |> Seq.map (fun h -> h.ComponentModel)
                         |> Seq.map (fun h ->
                                        let dep = h.Dependencies 
                                                  |> Seq.filter (fun m -> m.DependencyType <> DependencyType.Parameter)
                                                  |> Seq.map (fun m -> k.GetHandler(m.DependencyKey).ComponentModel)
                                                  |> Seq.map describe
                                        describe h,dep)
                         |> dict
    dependencyDict.ToVertexAndEdgeListGraph(fun kv -> kv.Value |> Seq.map (fun n -> SEquatableEdge(kv.Key,n)))

[<Test>]
let cycleInQuickgraph() =
    use c = new WindsorContainer()
    let reg = [|
                Component.For<IComp>().ImplementedBy<CompA>().Named("a")
                Component.For<IComp>().ImplementedBy<CompB>().Named("b")
                Component.For<IComp>().ImplementedBy<CompC>().Named("c")
              |] |> Array.cast
    c.Register(reg) |> ignore
    let graph = loadGraphFromKernel c.Kernel
    ()    