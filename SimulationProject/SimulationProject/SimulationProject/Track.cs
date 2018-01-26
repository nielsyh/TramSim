using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimulationProject
{
    class TrackDefault
    {
        public  EndStation centraal = new EndStation("Utrecht Centraal", 1);
        public  SubStation vaartscherijn = new SubStation("Vaartsche Rijn", 2);
        public  SubStation galgenwaard = new SubStation("Galgenwaard", 3);
        public  SubStation krommerijn = new SubStation("Kromme Rijn", 4);
        public  SubStation padualaan = new SubStation("Padualaan", 5);
        public  SubStation heidelberglaan = new SubStation("Heidelberglaan", 6);
        public  SubStation umc = new SubStation("UMC", 7);
        public  SubStation wkz = new SubStation("WKZ", 8);
        public  EndStation uithof = new EndStation("P+R De Uithof", 9);

        public List<Station> getStations()
        {
            List<Station> L = new List<Station>();
            L.Add(centraal); L.Add(vaartscherijn); L.Add(galgenwaard); L.Add(krommerijn); L.Add(padualaan);
            L.Add(heidelberglaan); L.Add(umc); L.Add(wkz); L.Add(uithof);
            return L;
        }
        
    }

     class TrackEasy
    {
        public  EndStation centraal = new EndStation("Utrecht Centraal", 1);
        public  SubStation vaartscherijn = new SubStation("Vaartsche Rijn", 2);
        public  EndStation uithof = new EndStation("P+R De Uithof", 3);
        
    }

}