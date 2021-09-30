open System.Threading
open System.Text
#time "on"
#r "nuget: Akka"
#r "nuget: Akka.FSharp"
#r "nuget: Akka.TestKit"
#r "nuget: Akka.Remote"

open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open Akka.TestKit
open System.Security.Cryptography

type Information = 
    | Inform of (int64*int64*int64)
    | CoinsFound of (string)
    | Test1 of (int)
    | RemoteDone
    | Start of (int)
   

//configuration
let configuration = 
    ConfigurationFactory.ParseString(
        @"akka {
            log-config-on-start : on
            stdout-loglevel : DEBUG
            loglevel : ERROR
            actor {
                provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                debug : {
                    receive : on
                    autoreceive : on
                    lifecycle : on
                    event-stream : on
                    unhandled : on
                }
            }
            remote {
                helios.tcp {
                    port = 8080
                    hostname = 10.20.108.13
                }
            }
        }")

let system = ActorSystem.Create("ClientBitcoin", configuration)


let RCA (mailbox:Actor<_>) =
    // printfn "inside this worker only"
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
        | Test1(no_of_zeros) -> 
        
            let rc=ranString
            let ufid = "agollam"
            let formattedString = ufid + "@" + rc

            let res= formattedString |> Encoding.ASCII.GetBytes |> (new SHA256Managed()).ComputeHash |> System.BitConverter.ToString
            let result1 = res.Replace("-", "")
            let os = formattedString + " " + result1
       
            let test = Check result1 no_of_zeros
            
            if test then
                remoteBoss<! os
       
            
        |_ -> ()

        return! loop()
    }
    loop()
let ip = fsi.CommandLineArgs.[1] |> string


let RBA = 
    spawn system "RBA"
    <| fun mailbox ->
    let ac = System.Environment.ProcessorCount |> int64
    let to1 = ac*1L
    let remoteChildActorssPool = 
            [1L .. to1]
            |> List.map(fun id -> spawn system (sprintf "Local_%d" id) RCA)

    let child = [|for rp in remoteChildActorssPool -> rp|]
    let workerSys = system.ActorOf(Props.Empty.WithRouter(Akka.Routing.RoundRobinGroup(child)))
    
    let sref12 = system.ActorSelection("akka.tcp://Bitcoin@"+ ip+":8779/user/Printer")

    let rec loop() =
        actor {
           
            let! (message: obj) = mailbox.Receive()
           
            let bossRef = mailbox.Sender()
            if (message :? string) then
                
                let x = "Remote String "+ (string message)
                sref12 <! x
            else
                
                let k = downcast message
                workerSys <! Test1(k)

              
            return! loop()
        }
    printf "Boss Started \n" 
    loop()


System.Console.ReadLine()      



