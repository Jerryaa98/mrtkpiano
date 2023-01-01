using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI;

public class ClickSong : MonoBehaviour
{
    public Material ColorMaterial;
    public string SongName;

    List<SongNote> keysToPlay;
    List<PianoCube> objects = new List<PianoCube>();
    int litKey = 0;
    float startTime;
    float speed = 0.01f;
    float width = 0.02f;
    float depth = 0.02f;
    float distance = 0.25f;

    void Start()
    {
        StartSong();
    }

    public void SetSongAndStart(string songName)
    {
        SongName = songName;
        StartSong();
    }

    public void StartSong()
    {
        if (string.IsNullOrWhiteSpace(SongName))
        {
            return;
        }
        keysToPlay = new List<SongNote>();
        if (objects.Any())
        {
            foreach (var obj in objects)
            {
                Destroy(obj.Cube);
            }
        }
        objects = new List<PianoCube>();

        var asset = Resources.Load<TextAsset>($"Songs/{SongName}");
        string[] lines = asset.text.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            string[] split = line.Trim().Split(',');
            keysToPlay.Add(new SongNote
            {
                Octave = split[0],
                Note = split[1],
                StartTime = float.Parse(split[2]),
                EndTime = float.Parse(split[3])
            });
        }
        litKey = 0;
        var currentKey = keysToPlay.Where(k => k.StartTime == 0);
        SetNewMaterial(currentKey);
        InstantiateKeys();
        startTime = Time.time;
    }

    public void ClickKey(GameObject obj)
    {
        if (litKey >= keysToPlay.Count)
        {
            return;
        }

        var foundNote = ClickedNotes.FirstOrDefault(n => string.Equals(n.Note.Note, obj.name, StringComparison.InvariantCultureIgnoreCase) &&
            string.Equals(n.Note.Octave, obj.transform.parent.name, StringComparison.InvariantCultureIgnoreCase)
            && !n.Clicked);
        if(foundNote != null)
        {
            Debug.Log(foundNote);
            foundNote.Clicked = true;
        }
        if(ClickedNotes.All(n => n.Clicked))
        {
            SetOriginalMaterial(ClickedNotes.Select(n => n.Note));
            JumpKey();

            litKey++;
            if (litKey >= keysToPlay.Count)
            {
                return;
            }

            //SetNewMaterial( /* todo: find next keys to play*/);
        }
    }

    void JumpKey()
    {
        foreach(var n in ClickedNotes)
        {
            var matchingObjectIndex = objects.FindIndex(o => o.Note == n.Note);
            Destroy(objects[matchingObjectIndex].Cube);
            objects.RemoveAt(matchingObjectIndex);
        }

        float lastPosition = 0f;
        foreach (var obj in objects)
        {
            obj.Cube.transform.localPosition = new Vector3(0f, distance, (-obj.Length * 0.5f) - lastPosition);
            lastPosition += obj.Length;
            obj.Position = obj.Cube.transform.localPosition;
        }
        startTime = Time.time;
    }

    void Update()
    {
        if (!objects.Any())
        {
            return;
        }

        var keyLength = objects.FirstOrDefault().Length;
        var journeyLength = keyLength * 0.06f;
        float distCovered = (Time.time - startTime) * speed;
        float fractionOfJourney = distCovered / journeyLength;

        foreach (var obj in objects)
        {
            obj.Cube.transform.localPosition = Vector3.Lerp(obj.Position, obj.Position + new Vector3(0, 0, journeyLength), fractionOfJourney);
            if (obj == objects.First())
            {
                if (fractionOfJourney > 1)
                {
                    obj.Cube.GetComponent<MeshRenderer>().material.color = Color.yellow;
                    obj.Cube.transform.localPosition = new Vector3(0, 0.25f, -obj.Length * 0.5f);
                }
            }
        }
    }

    void InstantiateKeys()
    {
        foreach (var key in keysToPlay)
        {
            var go = GetPianoKeyGO(key);
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.GetComponent<MeshRenderer>().material.color = Color.cyan;
            cube.transform.parent = go.transform.parent;
            var length = key.Length * 0.06f;
            cube.transform.localScale = new Vector3(width, length, depth);
            cube.transform.localPosition = new Vector3(0, distance, -(key.StartTime * 0.06f + (0.5f * length)));
            objects.Add(new PianoCube
            {
                Cube = cube,
                Length = length,
                Position = cube.transform.localPosition,
                Note = key
            });
        }
    }

    GameObject GetPianoKeyGO(SongNote currentKey)
    {
        return GameObject.Find($"{currentKey.Octave}/{currentKey.Note}/MovingKey");
    }

    void SetOriginalMaterial(IEnumerable<SongNote> keys)
    {
        foreach(var key in keys)
        {
            Debug.Log("setting original material");
            GetPianoKeyGO(key).GetComponent<MeshRenderer>().material = key.OriginaMaterial;
        }
    }

    void SetNewMaterial(IEnumerable<SongNote> keys)
    {
        foreach(var key in keys)
        {
            key.OriginaMaterial = GetPianoKeyGO(key).GetComponent<MeshRenderer>().material;
            GetPianoKeyGO(key).GetComponent<MeshRenderer>().material = ColorMaterial;
        }

        if (keys.Count() > 1)
        {
            ClickedNotes = keys.Select(k => new ClickedNote
            {
                Note = k,
                Clicked = false
            }).ToList();
        }
    }

    List<ClickedNote> ClickedNotes;

    class ClickedNote
    {
        public SongNote Note { get; set; }
        public bool Clicked { get; set; }
    }

    class PianoCube
    {
        public GameObject Cube;
        public float Length;
        public Vector3 Position;
        public SongNote Note;
    }

    class SongNote
    {
        public string Octave { get; set; }
        public string Note { get; set; }
        public float StartTime { get; set; }
        public float EndTime { get; set; }
        public float Length { get { return EndTime - StartTime; } }
        public Material OriginaMaterial { get; set; }
    }
}