using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FlaskeAutomaten
{
    class Buffer//Definerer en buffer klasse som bruges til at gemme en kø af strings og en max størrelse variable
    {
        private Queue<string> queue;//en kø til at opbevare strenge
        private int maxSize;//En variabel der opbevare den maksimale størrelse for køen

        public Buffer(int maxSize)//en konstrukter der opretter en buffor med en maksimalstørrelse
        {
            this.queue = new Queue<string>();
            this.maxSize = maxSize;
        }

        public void Push(string item)//En metode til at tilføje en ny streng til køen
        {
            lock (this.queue)//Låser køen for at sikre den
            {
                while (this.queue.Count >= this.maxSize)//hvis køen allerede er nået dens max størrelse venter tråden indtil der er plads til at tilføje en streng mere
                {
                    Monitor.Wait(this.queue);
                }
                this.queue.Enqueue(item);//Tilføjer en streng til køen
                Monitor.Pulse(this.queue);//Vækker den tråd der venter på køen
            }
        }

        public string Pull()//en metode til at trække en streng fra bufferen
        {
            lock (this.queue)//Låser køen for at sikre den
            {
                while (this.queue.Count == 0)//Hvis køen er tom venter den på der er en streng til rådighed
                {
                    Monitor.Wait(this.queue);
                }
                string item = this.queue.Dequeue();//Trækker den første stren ud af køen
                Monitor.Pulse(this.queue);//Vækker den tråd der venter på køen
                return item;//Returnerer strengen
            }
        }
    }

    class Producer//En klasse til producent der tilføjer øl og sodavand til bufferen
    {
        private Buffer buffer;//Laver bufferen der skal bruges af producent

        public Producer(Buffer buffer)//Konstruktør med buffer som argument
        {
            this.buffer = buffer;
        }

        public void Run()//MEtode som tilføjer øl og sodavanf til bufferen
        {
            for (int i = 1; i <= 10; i++)
            {
                string drink= "øl " + i;//Opretter en øl streng
                Console.WriteLine("Producer pushes " + drink);//Fortæller brugeren at producenten har pushet en øl til bufferen
                this.buffer.Push(drink);//Tilføjer øllen til bufferen

                drink = "sodavand " + i;//Opretter en sodavand streng
                Console.WriteLine("Producer pushes " + drink);//Fortæller brugeren at producenten har pushet en sodavand til bufferen
                this.buffer.Push(drink);//Tilføjer sodavand til bufferen

                Thread.Sleep(1000);
            }
        }
    }

    class Splitter
    {
        private Buffer inputBuffer;//Input buffer som modtager elementer fra producenten
        private Buffer outputBuffer1;//Output bufferen som indeholder øl elementer til forbruger 1
        private Buffer outputBuffer2;//Output bufferen som indeholder sodavand elementer til forbruger 2

        public Splitter(Buffer inputBuffer, Buffer outputBuffer1, Buffer outputBuffer2)
        {
            this.inputBuffer = inputBuffer;
            this.outputBuffer1 = outputBuffer1;
            this.outputBuffer2 = outputBuffer2;
        }

        public void Run()
        {
            while (true)
            {
                string drink = this.inputBuffer.Pull();//Puller et element fra input bufferen
                if (string.IsNullOrEmpty(drink))//Hvis elementet er null stopper tråden
                {
                    break;
                }
                Console.WriteLine("Splitter pulls " + drink);//Fortæller brugeren at splitteren puller et element

                if (drink.StartsWith("øl"))//Hvis elementet er en øl indsætter den det i outputbuffer 1
                {
                    Console.WriteLine("Splitter pushes " + drink + " to øl båndet");
                    this.outputBuffer1.Push(drink);
                }
                else if (drink.StartsWith("sodavand"))//Hvis elementet er sodavand indsætter den det i outputbuffer 2
                {
                    Console.WriteLine("Splitter pushes " + drink + " to sodavands båndet");
                    this.outputBuffer2.Push(drink);
                }
            }
        }
    }

    class Consumer
    {
        private string userName;//Navn på brugeren
        private Buffer buffer;//Bufferen som brugeren skal trække fra

        public Consumer(string userName, Buffer buffer)
        {
            this.userName = userName;
            this.buffer = buffer;
        }

        public void Run()
        {
            while (true)
            {
                string drink = this.buffer.Pull();//Puller et element fra bufferen
                if (string.IsNullOrEmpty(drink))//Hvis elementet er null stopper tråden
                {
                    break;
                }
                Console.WriteLine(this.userName + " pulls " + drink + "");//Udskriver navnet på brugeren og elementet som bliver pullet fra bufferen
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Buffer inputBuffer = new Buffer(10);//Input buffer til at modtage data fra producenten
            Buffer outputBuffer1 = new Buffer(5);//Buffer til øl consumeren
            Buffer outputBuffer2 = new Buffer(5);//Buffer til sodavands consumeren

            Producer producer = new Producer(inputBuffer);//Opretter et producer objekt som pusher data til input buffer
            Splitter splitter = new Splitter(inputBuffer, outputBuffer1, outputBuffer2);//Opretter et splitter objekt som tager data fra input buffer og sender øl og sodavand til outputbuffersne
            Consumer consumer1 = new Consumer("Øl consumer", outputBuffer1);//Opretter et øl consumer objekt der tager øl fra outputbuffer1
            Consumer consumer2 = new Consumer("Sodavand consumer", outputBuffer2);//Opretter et sodavands consumer objekt som tager fra outputbuffer2
            
            Thread producerThread = new Thread(producer.Run);//Opretter en tråd til at køre producentens run metode
            Thread splitterThread = new Thread(splitter.Run);//Opretter en tråd til at køre splitterens run metode
            Thread consumerThread1 = new Thread(consumer1.Run);//Opretter en tråd til at køre øl consumerens run metode
            Thread consumerThread2 = new Thread(consumer2.Run);//Opretter en tråd til at køre sodavand consumerens run metode

            //Starter trådene
            producerThread.Start();
            splitterThread.Start();
            consumerThread1.Start();
            consumerThread2.Start();


            producerThread.Join();//Venter på at producer tråden er færdig
            inputBuffer.Push(null);//Tilføjer en null til inputbuffer så splitter tråden ved at der ikke kommer mere data
            splitterThread.Join();//Venter på at splitter tråden er færdig
            outputBuffer1.Push(null);//Tilføjer en null til outputbuffer 1 så øl consumeren ved at der ikke kommer mere data
            outputBuffer2.Push(null);//Tilføjer en null til outputbuffer 2 så sodavands consumeren ved at der ikke kommer mere data
            consumerThread1.Join();//Venter på at øl consumer tråden er færdig
            consumerThread2.Join();//Venter på at sodavands consumer tråden er færdig

            Console.ReadLine();
        }
    }
}
