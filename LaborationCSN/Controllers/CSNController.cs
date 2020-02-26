using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Xml.Linq;

namespace LaborationCSN.Controllers
{
    public class CSNController : Controller
    {
        SQLiteConnection sqlite;

        public CSNController()
        {
            string path = HostingEnvironment.MapPath("/db/");
            sqlite = new SQLiteConnection($@"DataSource={path}\csn.sqlite");

        }
        XElement SQLResult(string query, string root, string nodeName)
        {
            sqlite.Open();

            var adapt = new SQLiteDataAdapter(query, sqlite);
            var ds = new DataSet(root);
            adapt.Fill(ds, nodeName);
            XElement xe = XElement.Parse(ds.GetXml());

            sqlite.Close();
            return xe;
        }


        //
        // GET: /Csn/Test
        // 
        // Testmetod som visar på hur ni kan arbeta från SQL till XML till
        // presentations-xml som sedan används i vyn.
        // Lite överkomplicerat för just detta enkla fall men visar på idén.
        public ActionResult Test()
        {
            string query = @"SELECT a.Arendenummer, s.Beskrivning, SUM(((Sluttid-starttid +1) * b.Belopp)) as Summa
                            FROM Arende a, Belopp b, BeviljadTid bt, BeviljadTid_Belopp btb, Stodform s, Beloppstyp blt
                            WHERE a.Arendenummer = bt.Arendenummer AND s.Stodformskod = a.Stodformskod
                            AND btb.BeloppID = b.BeloppID AND btb.BeviljadTidID = bt.BeviljadTidID AND b.Beloppstypkod = blt.Beloppstypkod AND b.BeloppID LIKE '%2009'
							Group by a.Arendenummer
							Order by a.Arendenummer ASC";
            XElement test = SQLResult(query, "BeviljadeTider2009", "BeviljadTid");
            XElement summa = new XElement("Total",
                (from b in test.Descendants("Summa")
                 select (int)b).Sum());
            test.Add(summa);

            // skicka presentations xml:n till vyn /Views/Csn/Test,
            // i vyn kommer vi åt den genom variabeln "Model"
            return View(test);
        }

        //
        // GET: /Csn/Index

        public ActionResult Index()
        {
            return View();
        }


        //
        // GET: /Csn/Uppgift1

        public ActionResult Uppgift1()
        {
            string query1 = @"SELECT a.Arendenummer, UtbetDatum, UtbetStatus, b.Belopp
                            FROM Utbetalning u, Utbetalningsplan up, Arende a, UtbetaldTid ut, UtbetaldTid_Belopp utb, Belopp b
                            WHERE a.Arendenummer = up.Arendenummer
                            AND u.UtbetPlanID = up.UtbetPlanID
                            AND u.UtbetID = ut.UtbetID
                            AND ut.UtbetTidID = utb.UtbetaldTidID
                            AND utb.BeloppID = b.BeloppID
                            ORDER BY a.Arendenummer";



            XElement arendenUtbet = SQLResult(query1, "AllaÄrendenUtbetalningar", "Utbetalning");

            XElement summaArenden =
                new XElement("SummaUtbet",
                    new XElement("Arende", new XAttribute("Arende", 14), 
                        new XElement("Totalbelopp", (from utbet in arendenUtbet.Descendants("Utbetalning")
                                                     where (int)utbet.Element("Arendenummer") == 14
                                                     select (int)utbet.Element("Belopp")).Sum()),
                        new XElement("PlaneradSumma", (from utbet in arendenUtbet.Descendants("Utbetalning")
                                                   where (int)utbet.Element("Arendenummer") == 14 && (string)utbet.Element("UtbetStatus") == "Planerad"
                                                   select (int)utbet.Element("Belopp")).Sum()),
                        new XElement("UtbetaldSumma", (from utbet in arendenUtbet.Descendants("Utbetalning")
                                                   where (int)utbet.Element("Arendenummer") == 14 && (string)utbet.Element("UtbetStatus") == "Utbetald"
                                                   select (int)utbet.Element("Belopp")).Sum())),

                new XElement("Arende", new XAttribute("Arende", 15),
                  new XElement("Totalbelopp", (from utbet in arendenUtbet.Descendants("Utbetalning")
                                               where (int)utbet.Element("Arendenummer") == 15
                                               select (int)utbet.Element("Belopp")).Sum()),
                  new XElement("PlaneradSumma", (from utbet in arendenUtbet.Descendants("Utbetalning")
                                                 where (int)utbet.Element("Arendenummer") == 15 && (string)utbet.Element("UtbetStatus") == "Planerad"
                                                 select (int)utbet.Element("Belopp")).Sum()),
                  new XElement("UtbetaldSumma", (from utbet in arendenUtbet.Descendants("Utbetalning")
                                                 where (int)utbet.Element("Arendenummer") == 15 && (string)utbet.Element("UtbetStatus") == "Utbetald"
                                                 select (int)utbet.Element("Belopp")).Sum())),

              new XElement("Arende", new XAttribute("Arende", 16),
                  new XElement("Totalbelopp", (from utbet in arendenUtbet.Descendants("Utbetalning")
                                               where (int)utbet.Element("Arendenummer") == 16
                                               select (int)utbet.Element("Belopp")).Sum()),
                  new XElement("PlaneradSumma", (from utbet in arendenUtbet.Descendants("Utbetalning")
                                                 where (int)utbet.Element("Arendenummer") == 16 && (string)utbet.Element("UtbetStatus") == "Planerad"
                                                 select (int)utbet.Element("Belopp")).Sum()),
                  new XElement("UtbetaldSumma", (from utbet in arendenUtbet.Descendants("Utbetalning")
                                                 where (int)utbet.Element("Arendenummer") == 16 && (string)utbet.Element("UtbetStatus") == "Utbetald"
                                                 select (int)utbet.Element("Belopp")).Sum())));


            arendenUtbet.Add(summaArenden);

            return View(arendenUtbet);
        }


        //
        // GET: /Csn/Uppgift2

        public ActionResult Uppgift2()
        {
            return View();
        }

        //
        // GET: /Csn/Uppgift3

        public ActionResult Uppgift3()
        {
            return View();
        }
    }
}