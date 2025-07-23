using LSL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using XCharts;
using XCharts.Runtime;

public class VRButtonLSLOutlet : MonoBehaviour
{
    public static VRButtonLSLOutlet Instance;

    string StreamName = "VRStream";
    string StreamType = "Markers";
    private StreamOutlet outlet;
    private StreamOutlet outlet2;
    private StreamOutlet outlet3;
    private string[] sample = { "" };
    private double[] positionSample = new double[10];

    private Transform headTransform;
    private Transform leftHandTransform;
    private Transform rightHandTransform;

    private double[] avatarSample = new double[46]; // 15 joints * 3 + 1 timestamp
    private readonly OVRSkeleton.BoneId[] trackedBones = new OVRSkeleton.BoneId[]
    {
        OVRSkeleton.BoneId.Body_Head,
        OVRSkeleton.BoneId.Body_Neck,
        OVRSkeleton.BoneId.Body_LeftShoulder,
        OVRSkeleton.BoneId.Body_RightShoulder,
        OVRSkeleton.BoneId.Body_SpineLower,
        OVRSkeleton.BoneId.Body_SpineMiddle,
        OVRSkeleton.BoneId.Body_Hips,
        OVRSkeleton.BoneId.FullBody_LeftUpperLeg,
        OVRSkeleton.BoneId.FullBody_RightUpperLeg,
        OVRSkeleton.BoneId.FullBody_LeftLowerLeg,
        OVRSkeleton.BoneId.FullBody_RightLowerLeg,
        OVRSkeleton.BoneId.FullBody_LeftFootAnkle,
        OVRSkeleton.BoneId.FullBody_RightFootAnkle,
        OVRSkeleton.BoneId.FullBody_LeftHandWrist,
        OVRSkeleton.BoneId.FullBody_RightHandWrist
    };
    private OVRSkeleton bodySkeleton;



    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this.gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        var hash = new Hash128();
        hash.Append(StreamName);
        hash.Append(StreamType);
        hash.Append(gameObject.GetInstanceID());

        StreamInfo streamInfo = new StreamInfo(StreamName, StreamType, 1, LSL.LSL.IRREGULAR_RATE, channel_format_t.cf_string, hash.ToString());
        outlet = new StreamOutlet(streamInfo);
        //Debug.Log("LSL stream created: " + StreamName);

        var hash2 = new Hash128();
        hash2.Append("VRPositionStream");
        hash2.Append("PositionStream");
        hash2.Append(gameObject.GetInstanceID());

        StreamInfo streamInfo2 = new StreamInfo("VRPositionStream", "PositionStream", 10, LSL.LSL.IRREGULAR_RATE, channel_format_t.cf_double64, hash2.ToString());
        outlet2 = new StreamOutlet(streamInfo2);
        //Debug.Log("LSL position stream created: " + "VRPositionStream");


        var hash3 = new Hash128();
        hash2.Append("VRAvatarPositionStream");
        hash2.Append("AvatarPositionStream");
        hash2.Append(gameObject.GetInstanceID());

        StreamInfo streamInfo3 = new StreamInfo("VRAvatarPositionStream", "AvatarPositionStream", 46, LSL.LSL.IRREGULAR_RATE, channel_format_t.cf_double64, hash3.ToString());
        outlet3 = new StreamOutlet(streamInfo3);

    }

    void Update()
    {
        if (headTransform && leftHandTransform && rightHandTransform)
        {
            Vector3 h = headTransform.position;
            Vector3 l = leftHandTransform.position;
            Vector3 r = rightHandTransform.position;

            positionSample[0] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            positionSample[1] = h.x;
            positionSample[2] = h.y;
            positionSample[3] = h.z;

            positionSample[4] = l.x;
            positionSample[5] = l.y;
            positionSample[6] = l.z;

            positionSample[7] = r.x;
            positionSample[8] = r.y;
            positionSample[9] = r.z;

            outlet2?.push_sample(positionSample);

            if (bodySkeleton != null &&
                bodySkeleton.IsInitialized &&
                bodySkeleton.Bones != null &&
                bodySkeleton.Bones.Count > 0 &&
                bodySkeleton.IsDataValid &&
                bodySkeleton.IsDataHighConfidence)
            {
                avatarSample[0] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                int idx = 1;

                foreach (var boneId in trackedBones)
                {
                    OVRBone bone = bodySkeleton.Bones
                        .FirstOrDefault(b => b.Id == boneId);

                    if (bone != null && bone.Transform != null)
                    {
                        Vector3 p = bone.Transform.position;
                        avatarSample[idx++] = p.x;
                        avatarSample[idx++] = p.y;
                        avatarSample[idx++] = p.z;
                    }
                    else
                    {
                        avatarSample[idx++] = 0;
                        avatarSample[idx++] = 0;
                        avatarSample[idx++] = 0;
                    }
                }

                outlet3?.push_sample(avatarSample);
            }

        }
    }

    public void SendMarker(string label)
    {
        if (outlet != null)
        {
            sample[0] = label;
            outlet.push_sample(sample);
            //Debug.Log("LSL marker sent: " + label);
        }
        else
        {
            //Debug.Log("LSL outlet not initialized");
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        //Debug.Log("Scene loaded: " + scene.name);
        SendMarker("Scene Entered: " + scene.name);

        headTransform = GameObject.Find("CenterEyeAnchor")?.transform;
        leftHandTransform = GameObject.Find("LeftHandAnchor")?.transform;
        rightHandTransform = GameObject.Find("RightHandAnchor")?.transform;

        bodySkeleton = FindObjectOfType<OVRSkeleton>();
        int jointCount = trackedBones.Length;
        avatarSample = new double[1 + jointCount * 3];
    }

    private void OnDestroy()
    {
        if (outlet != null)
        {
            outlet.Close();
            outlet = null;
        }

        if (outlet2 != null)
        {
            outlet2.Close();
            outlet2 = null;
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
