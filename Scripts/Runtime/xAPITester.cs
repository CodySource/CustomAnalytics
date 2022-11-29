using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using Newtonsoft.Json;

public class xAPITester : MonoBehaviour
{
    public enum Verbs { _Registered, _Attempted, _Completed, _Failed, _Passed, Experienced, Answered, Viewed, Watched, Interacted, Progressed };

    public string URL;
    public string username;
    public string password;

    public string ISO8601_Timestamp => System.DateTime.UtcNow.ToString("O");

    public Dictionary<Verbs, string> verbURL = new Dictionary<Verbs, string>
    {
        { Verbs._Attempted, "http://adlnet.gov/expapi/verbs/attempted" }
    };

    public void Export()
    {
        Verbs _verb = Verbs._Attempted;
        string _name = "Test Actor";
        string _uid = "86753098";
        object obj = new
        {
            actor = new
            {
                objectType = "Agent",
                name = _name,
                account = new
                {
                    name = _uid,
                    //  This homepage should be populated
                    homePage = "http://test.com"
                }
            },
            timestamp = ISO8601_Timestamp,
            version = "1.0.3",
            verb = new
            {
                id = verbURL[_verb],
                display = new
                {
                    _enUS = _verb.ToString().Replace("_", "")
                }
            },
            _object = new
            {
                //  This id should be populated
                id = "http://test.com/00000000",
                definition = new
                {
                    name = new
                    {
                        _enUS = "Activity Name"
                    },
                    description = new
                    {
                        _enUS = "Optional description."
                    }
                },
                objectType = "Activity"
            }
        };
        string json = JsonConvert.SerializeObject(obj)
            .Replace("_enUS","en-US")
            .Replace("_object","object");
        Debug.Log("Serialized Object -- "+json);
        StartCoroutine(_xAPI_Export(json));
    }

    /// <summary>
    /// Performs the actual object eqport
    /// </summary>
    internal IEnumerator _xAPI_Export(string pJSON)
    {
        UnityWebRequest _request = UnityWebRequest.Put(URL, Encoding.UTF8.GetBytes(pJSON));
        _request.method = "POST";
        string authorization = $"Basic {System.Convert.ToBase64String(Encoding.UTF8.GetBytes(username + ":" + password))}";
        Debug.Log("Authorization -- " + authorization);
        _request.SetRequestHeader("Authorization", authorization);
        _request.SetRequestHeader("X-Experience-API-Version", "1.0.3");
        _request.SetRequestHeader("Content-Type", "application/json");
        yield return _request.SendWebRequest();
        if (_request.result == UnityWebRequest.Result.Success) Debug.Log(_request.downloadHandler.text);
        else Debug.Log(_request.error);
        _request.Dispose();
    }

    private void Start()
    {
        Export();
    }
}