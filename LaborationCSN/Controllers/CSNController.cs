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
            string utbetalningar = @"SELECT DISTINCT a.Arendenummer, UtbetDatum, UtbetStatus, SUM((ut.Sluttid-ut.Starttid + 1) * b.belopp) OVER (PARTITION BY UtbetDatum, a.Arendenummer ORDER BY a.Arendenummer) AS Summa
                            FROM Utbetalning u, Utbetalningsplan up, Arende a, UtbetaldTid ut, UtbetaldTid_Belopp utb, Belopp b
                            WHERE a.Arendenummer = up.Arendenummer
                            AND u.UtbetPlanID = up.UtbetPlanID AND u.UtbetID = ut.UtbetID AND ut.UtbetTidID = utb.UtbetaldTidID AND utb.BeloppID = b.BeloppID
                            order by a.Arendenummer";
         
            XElement arendenUtbet = SQLResult(utbetalningar, "Utbetalningar", "Utbetalning");

            var q = new XElement("Summeringar",
            from arende in arendenUtbet.Descendants("Utbetalning").Elements("Arendenummer")
            group arende by arende.Value into groupedByArende
            select new XElement("Arende",
                      new XElement("Arendenummer", groupedByArende.Key),
                      new XElement("Totalsumma", (from u in arendenUtbet.Descendants("Utbetalning")
                                                  where u.Element("Arendenummer").Value == groupedByArende.Key
                                                  select (int)u.Element("Summa")).Sum()),
                      new XElement("Utbetalt", (from u in arendenUtbet.Descendants("Utbetalning")
                                                where u.Element("Arendenummer").Value == groupedByArende.Key
                                                && (string)u.Element("UtbetStatus") == "Utbetald"
                                                select (int)u.Element("Summa")).Sum()),
                      new XElement("Planerat", (from u in arendenUtbet.Descendants("Utbetalning")
                                                where u.Element("Arendenummer").Value == groupedByArende.Key
                                                && (string)u.Element("UtbetStatus") == "Planerad"
                                                select (int)u.Element("Summa")).Sum()),
                      new XElement("Utbetalningar", from u in arendenUtbet.Descendants("Utbetalning")
                                                    where u.Element("Arendenummer").Value == groupedByArende.Key
                                                    select new XElement("Utbetalning",
                                                                     u.Element("UtbetDatum"),
                                                                     u.Element("UtbetStatus"),
                                                                     u.Element("Summa")))));
            return View(q);
        }


        //
        // GET: /Csn/Uppgift2

        public ActionResult Uppgift2()
        {
            string query2 = @"SELECT u.UtbetDatum, btk.Beskrivning, SUM((ut.Sluttid-ut.Starttid + 1) * b.belopp) AS Belopp
                            FROM Arende a, Utbetalningsplan up, Utbetalning u, UtbetaldTid ut, UtbetaldTid_Belopp utb, Belopp b, Beloppstyp btk
                            WHERE a.Arendenummer = up.Arendenummer
                            AND u.UtbetPlanID = up.UtbetPlanID AND u.UtbetID = ut.UtbetID AND ut.UtbetTidID = utb.UtbetaldTidID
                            AND utb.BeloppID = b.beloppID AND b.Beloppstypkod = btk.Beloppstypkod AND u.UtbetStatus = 'Utbetald'
                            GROUP BY u.UtbetDatum, btk.Beskrivning";
       
            XElement allaUtbet = SQLResult(query2, "UtbetPerDatum", "Utbetalning");

            var q2 = new XElement("Summeringar",
                            from utbetalningar in allaUtbet.Descendants("Utbetalning").Elements("UtbetDatum")
                            group utbetalningar by utbetalningar.Value into groupedByDate
                            select new XElement("Datum",
                                   new XElement("UtbetDatum", groupedByDate.Key),
                                   new XElement("Totalsumma", (from u in allaUtbet.Elements("Utbetalning")
                                                               where (string)u.Element("UtbetDatum") == groupedByDate.Key
                                                               select (int)u.Element("Belopp")).Sum()),
                                   new XElement("Utbetalningar", from u in allaUtbet.Elements("Utbetalning")
                                                                 where (string)u.Element("UtbetDatum") == groupedByDate.Key
                                                                 select new XElement("Utbetalning",
                                                                                             u.Element("Beskrivning"),
                                                                                             u.Element("Belopp")))));
            return View(q2);
        }

        //
        // GET: /Csn/Uppgift3

        public ActionResult Uppgift3()
        {
            string query3 = @"SELECT DISTINCT Starttid, Sluttid, s.Beskrivning, SUM(((Sluttid-Starttid +1) * b.Belopp)) as Summa
                            FROM BeviljadTid bt, BeviljadTid_Belopp btb, Belopp b, Arende a, Stodform s
                            WHERE btb.BeviljadTidID = bt.BeviljadTidID 
                            AND btb.BeloppID = b.BeloppID AND bt.Arendenummer = a.Arendenummer AND a.Stodformskod = s.Stodformskod
                            GROUP BY Starttid, Sluttid, Beskrivning
                            ORDER BY Beskrivning";

            XElement beviljadeTider = SQLResult(query3, "BeviljadeTider", "BeviljadTid");
            return View(beviljadeTider);
        }
    }
}