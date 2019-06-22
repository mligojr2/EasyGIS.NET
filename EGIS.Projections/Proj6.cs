﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EGIS.Projections
{
    internal class Proj6
    {
        //public static 

        public abstract class CRS : ICRS
        {
            public string WKT
            {
                get;
                internal set;
            }

            public string Name
            {
                get;
                internal set;
            }

            public string Id
            {
                get;
                internal set;
            }

            public string Authority
            {
                get;
                internal set;
            }

            public bool IsEquivalent(ICRS other)
            {
                if (other == null) return false;

                IntPtr pjThis = Proj6Native.proj_create(IntPtr.Zero, this.WKT);
                IntPtr pjOther = Proj6Native.proj_create(IntPtr.Zero, other.WKT);
                try
                {
                    if (pjThis != IntPtr.Zero && pjOther != IntPtr.Zero)
                    {
                        return Proj6Native.proj_is_equivalent_to(pjThis, pjOther, Proj6Native.PJ_COMPARISON_CRITERION.PJ_COMP_EQUIVALENT) != 0;
                    }
                }
                finally
                {
                    if (pjThis != IntPtr.Zero) Proj6Native.proj_destroy(pjThis);
                    if (pjOther != IntPtr.Zero) Proj6Native.proj_destroy(pjOther);
                }
                return false;

            }


            public static CRS FromWKT(string wkt)
            {
                IntPtr p = Proj6Native.proj_create(IntPtr.Zero, wkt);
                try
                {
                    Proj6Native.PJ_TYPE pType = Proj6Native.proj_get_type(p);
                    //Console.Out.WriteLine("pType=" + pType);
                    string name = Proj6Native.GetName(p);
                    string authName = Proj6Native.GetAuthName(p);
                    string id = Proj6Native.ProjGetIdCode(p);
                        
                    if (pType == Proj6Native.PJ_TYPE.PJ_TYPE_GEOGRAPHIC_2D_CRS)
                    {
                        return new GeographicCRS()
                        {
                            Id = id,
                            Name = name,
                            Authority = authName,
                            WKT = wkt
                        };
                    }
                    else if (pType == Proj6Native.PJ_TYPE.PJ_TYPE_PROJECTED_CRS)
                    {
                        
                        return new ProjectedCRS()
                        {
                            Id = id,
                            Name = name,
                            Authority = authName,
                            WKT = wkt,
                            UnitsToMeters = 1
                        };
                        
                    }
                    else if (pType == Proj6Native.PJ_TYPE.PJ_TYPE_BOUND_CRS)
                    {
                        if (wkt.IndexOf("PROJECTION", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return new ProjectedCRS()
                            {
                                Id = id,
                                Name = name,
                                Authority = authName,
                                WKT = wkt,
                                UnitsToMeters = 1
                            };
                        }
                        else
                        {
                            return new GeographicCRS()
                            {
                                Id = id,
                                Name = name,
                                Authority = authName,
                                WKT = wkt
                            };
                        }                                                
                    }
                    else
                    {
                        //Console.Out.WriteLine("pType = " + pType);
                    }

                    return null;
                }
                finally
                {
                    if(p != IntPtr.Zero) Proj6Native.proj_destroy(p);
                }

            }

        }

        public class GeographicCRS : CRS, IGeographicCRS
        {            
        }

        public class ProjectedCRS : CRS, IProjectedCRS
        {
            public double UnitsToMeters
            {
                get;
                internal set;
            }
        }


        public class CoordinateTransformation : ICoordinateTransformation
        {
            IntPtr pjNative = IntPtr.Zero;

            public CoordinateTransformation(ICRS source, ICRS target)
            {
                this.SourceCRS = source;
                this.TargetCRS = target;

                pjNative = Proj6Native.proj_create_crs_to_crs(IntPtr.Zero, source.WKT, target.WKT, IntPtr.Zero);

                if (pjNative == IntPtr.Zero)
                {
                    throw new Exception("Could not create coordinate transformation");
                }
                IntPtr pn = Proj6Native.proj_normalize_for_visualization(IntPtr.Zero, pjNative);
                Proj6Native.proj_destroy(pjNative);
                pjNative = IntPtr.Zero;                
                if (pn == IntPtr.Zero)
                {
                    throw new Exception("Could not create coordinate transformation - proj_normalize_for_visualization returned zero");
                }
                pjNative = pn;
            }

            public ICRS SourceCRS
            {
                get;
                private set;            
            }

            public ICRS TargetCRS
            {
                get;
                private set;            
            }

            public int Transform(double[] points, int pointCount)
            {
                int count = Proj6Native.proj_trans_generic(pjNative, Proj6Native.PJ_DIRECTION.PJ_FWD, points, pointCount);
                return count;
            }

            public int Transform(double[] points, int startIndex, int pointCount)
            {
                throw new NotImplementedException();
            }

            public unsafe int Transform(double* points, int pointCount)
            {
                int count = Proj6Native.proj_trans_generic(pjNative, Proj6Native.PJ_DIRECTION.PJ_FWD, points, pointCount);
                return count;
            }



            public void Dispose()
            {
                if (pjNative != IntPtr.Zero)
                {
                    Proj6Native.proj_destroy(pjNative);
                }
                pjNative = IntPtr.Zero;
            }
        }

    }

    

}