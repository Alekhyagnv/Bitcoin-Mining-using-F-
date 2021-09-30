#time "on"
#r "nuget: Akka"
#r "nuget: Akka.FSharp"
#r "nuget: Akka.Remote"
#r "nuget: Akka.TestKit"
open System.Text
open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open System.Threading
open Akka.TestKit
open System.Security.Cryptography

// Configuration
let configuration = 
    ConfigurationFactory.ParseString(
        @"akka {
            log-config-on-start : on
            stdout-loglevel : DEBUG
            loglevel : ERROR
            actor {
                provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
            }
            remote.helios.tcp {
                transport-protocol = tcp
                port = 8779
                hostname = 10.20.108.22
            }
        }")

let system = ActorSystem.Create("Bitcoin", configuration)
type Information = 
    | Inform of (int*int)
    | CoinsFound of (string)
    | Input1 of (int*int)
    | Start of (int)
    | RemoteDone

let printerActor (mailbox:Actor<_>) = 
    
    let rec loop () = actor {
        let! (message:string) = mailbox.Receive()
        printfn "%s" message
        return! loop()
    }
    loop()
let printerRef = spawn system "Printer" printerActor


let WA (mailbox:Actor<_>) =
    
    let rec loop () = actor {
        let! (message : Information) = mailbox.Receive()
        let remoteBoss = mailbox.Sender()
        let x : Information = message


     
        
        let Check (str: string) (zeroes: int) : Boolean =
        
            let mutable v = true

            for i1 in 0..zeroes-1 do
                if (str.[i1] <> '0') then v <- false
 
            v

        
        let ranString = 
            let r = Random()
            let chars = Array.concat([[|'a' .. 'z'|];[|'A' .. 'Z'|];[|'0' .. '9'|]])
            let s1 = Array.length chars in
            String(Array.init 8 (fun _ -> chars.[r.Next s1]))
        


        match x with
        |Inform(no_of_rand,no_of_zeros) -> 
            
            let rc=ranString
            let ufid = "agollam"
            let formattedString = ufid + "@" + rc
            let res= formattedString |> Encoding.ASCII.GetBytes |> (new SHA256Managed()).ComputeHash |> System.BitConverter.ToString
            let result1 = res.Replace("-", "")
            let p= formattedString+" "+result1
            let test = Check result1 no_of_zeros
            if test then
                remoteBoss<! CoinsFound(p)
       
            
        |_ -> ()

        return! loop()
    }
    loop()


let BA (mailbox:Actor<_>) = 
    let actcount = System.Environment.ProcessorCount |> int64 
    let totalactors = actcount*125000L // processors*125
    let split = totalactors*1L // processors*125
    let mutable bitcoinsCount = 0

    let workerActorsPool = 
            [1L .. totalactors]
            |> List.map(fun id -> spawn system (sprintf "Local_%d" id) WA)

    let workerenum = [|for lp in workerActorsPool -> lp|]
    let workerSystem = system.ActorOf(Props.Empty.WithRouter(Akka.Routing.RoundRobinGroup(workerenum)))
    


    let int2String (x: int64) = string x

    let rec loop() = actor  {
        
        let! message = mailbox.Receive()

        match message with 
        | Input1(n, k) -> 
            let sref1 = system.ActorSelection("akka.tcp://ClientBitcoin@10.20.108.13:8080/user/RBA")

            let inline first act = act <! Inform(8,k)
            let inline second act = act <! (k)
            let splits = totalactors*2L
            for i = 1L to splits do
                let x = i % 2L
                
                match x with
                | 0L -> 
                    
                    first workerSystem
                | 1L -> 
                    
                    second sref1
                    
                |_ -> ()
                           
        | CoinsFound(complete) -> 
            printerRef <! "Local  "+complete
            bitcoinsCount <- bitcoinsCount + 1

            if bitcoinsCount = 10 then
                mailbox.Context.System.Terminate() |> ignore

        | _ -> ()
        return! loop()
    }
    loop()

let bosss = spawn system "boss" BA

let K1 = fsi.CommandLineArgs.[1] |> int
bosss <! Input1(8, K1)

system.WhenTerminated.Wait()