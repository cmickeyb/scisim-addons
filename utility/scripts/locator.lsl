// UTM coordinates of the bounding box used for gettysburg
float LEFTX =  305000.0000;
float RIGHTX = 313000.0000;
float LOWERY = 4405500.0000; 
float UPPERY = 4413500.0000;

integer zone = 18;
 
// Mapping of space to regions
integer RegionsPerSim = 8;
integer Regions = 32;
 
// Region names encode location: "Geography22 32"
string pattern = "Gettysburg(\\d)(\\d) (\\d)(\\d)";

string MapURL = "http://maps.google.com/maps?ll={0},{1}&spn=0.012656,0.013025&t=h&z=16";
 
// -----------------------------------------------------------------
// NAME: ConvertRegiontoUTM
//     coord == [ region, xpos, ypos ]
//     returns == [ easting, northing ]
// -----------------------------------------------------------------
list ConvertRegiontoUTM(list coord)
{
  string rname = llList2String(coord, 0);
  float xpos = llList2Float(coord,1);
  float ypos = llList2Float(coord,2);
 
  list tokens = osMatchString(rname,pattern,0);
  if (llGetListLength(tokens) == 0)
    return [];
 
  integer simX = llList2Integer(tokens,2);
  integer simY = llList2Integer(tokens,4);
  integer regX = llList2Integer(tokens,6);
  integer regY = llList2Integer(tokens,8);
 
  float metersX = (simX * RegionsPerSim + regX) * 256.0 + xpos;
  float metersY = (simY * RegionsPerSim + regY) * 256.0 + ypos;
  
 
  float multiX = metersX / (Regions * 256.0);
  float multiY = metersY / (Regions * 256.0);
  
  // Need to check the signs and offsets here, origin is supposed to
  // be lower left (UL_X, LR_Y)
    float utmX = LEFTX + multiX * (RIGHTX - LEFTX);
    float utmY = LOWERY + multiY * (UPPERY - LOWERY);
 
  llOwnerSay(osFormatString("UTM location is {0},{1}",[utmX,utmY]));

  return [utmX, utmY];
}
 
// -----------------------------------------------------------------
// NAME: ConvertUTMtoLatLon
//     utm == [ easting, northing ]
//     returns == [ latitude, longitude ]
// -----------------------------------------------------------------
list ConvertUTMtoLatLon(list utm)
{
  float latitude;
  float longitude;
 
  float easting = (float)llList2Float(utm,0);
  float northing = (float)llList2Float(utm,1);
 
  float const0 = 0.99960000000000004;
  float const1 = 6378137; // polar radius
  float const2 = 0.0066943799999999998;
  float const3 = const2 / (1 - const2);
  float const4 = (1 - llSqrt(1 - const2)) / (1 + llSqrt(1 - const2));
 
  float zonenorm = ((zone - 1) * 6 - 180) + 3;
  float northnorm = northing / const0;
  float nvar0 = northnorm / (const1 * (1 - const2 / 4 - (3 * const2 * const2) / 64 - (5 * llPow(const2,3) ) / 256));
  float nvar1 = nvar0 +
    ((3 * const4) / 2 - (27 * llPow(const4,3) ) / 32) * llSin(2 * nvar0) +
    ((21 * const4 * const4) / 16 - (55 * llPow(const4,4) ) / 32) * llSin(4 * nvar0) +
    ((151 * llPow(const4,3) ) / 96) * llSin(6 * nvar0);
 
  float nvar2 = const1 / llSqrt(1 - const2 * llSin(nvar1) * llSin(nvar1));
  float nvar3 = llTan(nvar1) * llTan(nvar1);
  float nvar4 = const3 * llCos(nvar1) * llCos(nvar1);
  float nvar5 = (const1 * (1 - const2)) / llPow(1 - const2 * llSin(nvar1) * llSin(nvar1), 1.5);
  float evar1 = (easting - 500000) / (nvar2 * const0);
 
  float latrad = nvar1 - ((nvar2 * llTan(nvar1)) / nvar5) * (((evar1 * evar1) / 2 - (((5 + 3 * nvar3 + 10 * nvar4) - 4 * nvar4 * nvar4 - 9 * const3) * llPow(evar1,4) ) / 24) + (((61 + 90 * nvar3 + 298 * nvar4 + 45 * nvar3 * nvar3) - 252 * const3 - 3 * nvar4 * nvar4) * llPow(evar1,6) ) / 720);
  latitude = latrad * RAD_TO_DEG;
 
  float lonrad = ((evar1 - ((1 + 2 * nvar3 + nvar4) * llPow(evar1,3) ) / 6) + (((((5 - 2 * nvar4) + 28 * nvar3) - 3 * nvar4 * nvar4) + 8 * const3 + 24 * nvar3 * nvar3) * llPow(evar1,5) ) / 120) / llCos(nvar1);
  longitude = zonenorm + lonrad * RAD_TO_DEG;
 
  return [latitude, longitude];
}
 
// XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
// STATE: default
// XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
 default
 {
   // ---------------------------------------------
   state_entry()
     {
       llSay(0,"running");
     }
 
   // ---------------------------------------------
   touch_start(integer i)
     {
       vector pos = llGetPos();
       string name = llGetRegionName();
       list utm = ConvertRegiontoUTM([name, pos.x, pos.y]);
       if (llGetListLength(utm) > 0)
         {
           list latlon = ConvertUTMtoLatLon(utm);
                     string msg = osFormatString("Gettysburg map at {0}, {1}",latlon);
                     string url = osFormatString(MapURL,latlon);
                     llLoadURL(llDetectedKey(0),msg,url);
         }
       else
         {
           llOwnerSay("you are not in gettysburg");
         }
     }
 }
 
 
// Local Variables: ***
// tab-width: 2 ***
// c-basic-offset: 2 ***
// End: ***
