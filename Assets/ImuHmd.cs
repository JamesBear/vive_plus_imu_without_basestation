//======================================================================================================
// Copyright 2016, NaturalPoint Inc.
//======================================================================================================

using System;
using System.Runtime.InteropServices;
using UnityEngine;


public class ImuHmd : MonoBehaviour
{
    
    private IntPtr m_sfs;
    
    private bool m_initialPoseSet = false;

    // foward, upward, left, left tilt
    Vector3[] desiredEulers = new Vector3[] { new Vector3(0, 0, 0), new Vector3(-90, 0, 0), new Vector3(0,-90,0), new Vector3(0, 0, -90) };
    Vector3[] actualEulers = new Vector3[4];

    enum State
    {
        RecordingForward = 0,
        RecordingUpward = 1,
        RecordingLeft = 2,
        RecordingLeftTilt = 3,
    }

    void Start()
    {
        
    }

    void OnEnable()
    {

        if (m_sfs == IntPtr.Zero) {
            Debug.Log("starting lpSensorFusion", this);
            m_sfs = NativeMethods.lpStartSensorFusion();
        }
            
        if (m_sfs == IntPtr.Zero) {
            Debug.LogError( "m_sfs == null " + m_sfs, this);
        } else {
            double invsq2 = 1 / Math.Sqrt(2);
            //double [] qAssembly = new double [] { 0, 1 / Math.Sqrt(2) - 0.014, 1 / Math.Sqrt(2) + 0.014, 0 };
            //double [] qAssembly = new double [] { invsq2, 0, invsq2, 0 };
            //double[] qAssembly = new double[] { 1, 0, 0, 0 };
            //double[] qAssembly = new double[] { invsq2, invsq2, 0, 0 };
            //double[] qAssembly = new double[] { invsq2, 0, 0, invsq2 }; // that's close
            //double[] qAssembly = new double[] { invsq2, -invsq2, 0, 0 };
            //double[] qAssembly = new double[] { invsq2, 0, 0, -invsq2 };
            double[] qAssembly = new double[] { 0, invsq2, invsq2, 0 }; // z's wrong
            //double[] qAssembly = new double[] { 0, invsq2, 0, invsq2 };
            //double[] qAssembly = new double[] { 0, 0, invsq2, invsq2 };
            NativeMethods.lpSetAssemblyRotation(m_sfs, qAssembly);
            Debug.Log("assembly rotation set!: " + convertFromLpsfQuaternion(qAssembly));

            
        }

        m_initialPoseSet = false;
    }


    void OnDisable()
    {
        if (m_sfs != IntPtr.Zero)
        {
            Debug.Log("Stopping sensorfusion", this);
            NativeMethods.lpStopSensorFusion(m_sfs);
            m_sfs = IntPtr.Zero;
        }
    }

    void ResetCenter()
    {
        NativeMethods.lpSetCurrentOrientation(m_sfs, convertToLpsfQuaternion(Quaternion.identity));
        //transform.GetChild(0).rotation = Quaternion.identity;
    }

    void Print()
    {
        double[] q = new double[] { 1, 0, 0, 0 };
        NativeMethods.lpGetOrientationOVR(m_sfs, q);
        m_baseRot = convertFromLpsfQuaternion(q);
        Debug.Log(m_baseRot.eulerAngles);

    }

    Quaternion m_baseRot = Quaternion.identity;

    void Update()
    {
        if (Input.GetMouseButtonUp(0))
        {
            ResetCenter();
        }
        if (Input.GetMouseButtonUp(1))
        {
            Print();
        }

        double[] q = new double[] { 1, 0, 0, 0 };
        NativeMethods.lpGetOrientationOVR(m_sfs, q);
        //NativeMethods.lpGetOrientation(m_sfs, 0.01, q);
        this.transform.localRotation = MapOrientation(convertFromLpsfQuaternion(q));

    }

    Quaternion MapOrientation(Quaternion rot)
    {
        return rot;
        Vector3 euler = rot.eulerAngles;
        Quaternion result = Quaternion.Euler(euler.y+euler.x, euler.z+euler.x, -euler.x);
        //Quaternion delta = rot*Quaternion.Inverse(m_baseRot);
        //Quaternion result = delta;
        //Quaternion result = rot;

        return result;
    }

    private enum NpHmdResult
    {
        OK = 0,
        InvalidArgument
    }


    private struct NpHmdQuaternion
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public NpHmdQuaternion( UnityEngine.Quaternion other )
        {
            this.x = other.x;
            this.y = other.y;
            this.z = other.z;
            this.w = other.w;
        }

        public static implicit operator UnityEngine.Quaternion( NpHmdQuaternion nphmdQuat )
        {
            return new UnityEngine.Quaternion
            {
                w = nphmdQuat.w,
                x = nphmdQuat.x,
                y = nphmdQuat.y,
                z = nphmdQuat.z
            };
        }
    }

    private UnityEngine.Quaternion convertFromLpsfQuaternion( [In] double[] q )
    {
        return new UnityEngine.Quaternion
        {
            w = (float)q[0],
            x = (float)q[1],
            y = (float)q[2],
            z = -(float)q[3]
        };
    }

    private double[] convertToLpsfQuaternion( UnityEngine.Quaternion q )
    {
        return new double []
         {
             q.w,
             q.x,
             q.y,
             -q.z
         };
    }

    private static class NativeMethods
    {
        //public const string NpHmdDllBaseName = "HmdDriftCorrection";
        //public const CallingConvention NpHmdDllCallingConvention = CallingConvention.Cdecl;

        //[DllImport( NpHmdDllBaseName, CallingConvention = NpHmdDllCallingConvention )]
        //public static extern NpHmdResult NpHmd_UnityInit();

        //[DllImport( NpHmdDllBaseName, CallingConvention = NpHmdDllCallingConvention )]
        //public static extern NpHmdResult NpHmd_Create( out IntPtr hmdHandle );

        //[DllImport( NpHmdDllBaseName, CallingConvention = NpHmdDllCallingConvention )]
        //public static extern NpHmdResult NpHmd_Destroy( IntPtr hmdHandle );

        //[DllImport( NpHmdDllBaseName, CallingConvention = NpHmdDllCallingConvention )]
        //public static extern NpHmdResult NpHmd_MeasurementUpdate( IntPtr hmdHandle, ref NpHmdQuaternion opticalOrientation, ref NpHmdQuaternion inertialOrientation, float deltaTimeSec );

        //[DllImport( NpHmdDllBaseName, CallingConvention = NpHmdDllCallingConvention )]
        //public static extern NpHmdResult NpHmd_GetOrientationCorrection( IntPtr hmdHandle, out NpHmdQuaternion correction );


        //public const string OvrPluginDllBaseName = "OVRPlugin";
        //public const CallingConvention OvrPluginDllCallingConvention = CallingConvention.Cdecl;

        //[DllImport( OvrPluginDllBaseName, CallingConvention = OvrPluginDllCallingConvention )]
        //public static extern Int32 ovrp_GetCaps();

        //[DllImport( OvrPluginDllBaseName, CallingConvention = OvrPluginDllCallingConvention )]
        //public static extern Int32 ovrp_SetCaps( Int32 caps );

        //[DllImport( OvrPluginDllBaseName, CallingConvention = OvrPluginDllCallingConvention )]
        //public static extern Int32 ovrp_SetTrackingIPDEnabled( Int32 value );

        public const string SensorFusionLibraryDllBaseName = "LPMS-VR";

        public const CallingConvention SensorFusionLibraryCallingConvention = CallingConvention.Winapi;

        [DllImport( SensorFusionLibraryDllBaseName, CallingConvention = SensorFusionLibraryCallingConvention )]
        public static extern IntPtr lpStartSensorFusion(byte [] str = null);

        [DllImport( SensorFusionLibraryDllBaseName, CallingConvention = SensorFusionLibraryCallingConvention )]
        public static extern IntPtr lpStopSensorFusion(IntPtr sfs);

        [DllImport( SensorFusionLibraryDllBaseName, CallingConvention = SensorFusionLibraryCallingConvention )]
        public static extern IntPtr lpSetCurrentOrientation(IntPtr sfs, [In] double[] q);

        [DllImport( SensorFusionLibraryDllBaseName, CallingConvention = SensorFusionLibraryCallingConvention )]
        public static extern IntPtr lpSetCurrentOrientationSoft(IntPtr sfs, [In] double[] q);

        [DllImport( SensorFusionLibraryDllBaseName, CallingConvention = SensorFusionLibraryCallingConvention )]
        public static extern IntPtr lpGetOrientationOVR(IntPtr sfs, [Out] double[] q);

        [DllImport( SensorFusionLibraryDllBaseName, CallingConvention = SensorFusionLibraryCallingConvention )]
        public static extern IntPtr lpGetOrientation(IntPtr sfs, double whenFromNow, [Out] double[] q);

        [DllImport( SensorFusionLibraryDllBaseName, CallingConvention = SensorFusionLibraryCallingConvention )]
        public static extern IntPtr lpSetAssemblyRotation(IntPtr sfs, [In] double[] q);
    }
    
}
