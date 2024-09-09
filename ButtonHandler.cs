using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using TMPro;

public class ButtonHandler : MonoBehaviour
{
    public Button submitButton;
    public ToggleGroup toggleGroup;

    public string flaskServer = "http://127.0.0.1:5001/generate_mesh";
    public string modelCoeffsPath = "../coeff_testing_framework/stage3_coeffs.pt";
    public string outputDirectory = "../coeff_testing_framework";
    public GameObject headModel;

    private static readonly HttpClient client = new HttpClient();

    private Dictionary<int, List<float>> emotionDict = new Dictionary<int, List<float>>()
    {
        { 0, new List<float> {
            0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0, 
            0, 0, 0, 0, 1.38f, 
            1.38f, 0, 0, 0, 0, 
            0, 0, 0, 0, 0,        
            0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0
            }
        },
        { 1, new List<float> {
            0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0, 
            0, 0, 0, 0, -0.73f, 
            -0.73f, 0, 0, 0, 0, 
            0, 0, 0, 0, 0,        
            0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0
            }
        },
        { 2, new List<float> {
            0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0,        
            0, 0.3f, -0.84f, 0, 0, 
            0, 0, 0, 0, 0
            }
        },
        { 3, new List<float> {
            0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0, 
            -0.11f, -0.19f, 1.52f, 0, 0,        
            0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0
            }
        },
        { 4, new List<float> {
            0, 0, 0, 0, 0, 
            0, 0, 0, 0, 1.47f, 
            0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0,        
            0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0
            }
        },
        { 5, new List<float> {
            0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0, 
            0, 0, 0, 1f, 0, 
            0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0,        
            0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0
            }
        },
        { 6, new List<float> {
            0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0,        
            0, 0, 0, 0, 0, 
            0, 0, 0, 0, 0
            }
        }
    };

    void Start()
    {
        // Click the submit button to submit the value of the slider
        submitButton.onClick.AddListener(OnSubmit);
    }

    async void OnSubmit()
    {
        Toggle selectedToggle = GetSelectedToggle();
        if (selectedToggle == null)
        {
            Debug.LogError("No toggle selected.");
            return;
        }

        int selectedIndex = System.Array.IndexOf(toggleGroup.GetComponentsInChildren<Toggle>(), selectedToggle);
        if (selectedIndex < 0 || !emotionDict.ContainsKey(selectedIndex))
        {
            Debug.LogError("Invalid toggle index or preset values not defined.");
            return;
        }
        List<float> changeValues = emotionDict[selectedIndex];
        Debug.Log(changeValues);

        // Passes the value of the expression basis and value to be changed into the function
        string result = await SendExpressionRequest(changeValues);
        Debug.Log(result);

        byte[] objFile = await DownloadObjFile(result);
        Mesh mesh = LoadMeshFromObj(objFile);
        ApplyMeshToModel(mesh);
    }

    Toggle GetSelectedToggle()
    {
        foreach (Toggle toggle in toggleGroup.GetComponentsInChildren<Toggle>())
        {
            if (toggle.isOn)
            {
                return toggle;
            }
        }
        return null;
    }

    [System.Serializable]
    public class RequestData
    {
        public string model_coeffs_path;
        public List<float> change_values;
        public string output_path;
    }

    [System.Serializable]
    public class ResponseData
    {
        public string saved_mesh;
    }

    // Function to send a request to the Python Flask backend
    async Task<string> SendExpressionRequest(List<float> changeValues)
    {
        RequestData data = new RequestData
        {
            model_coeffs_path = modelCoeffsPath,
            change_values = changeValues,
            output_path = outputDirectory
        };
        
        // JSON string to facilitate Flask app request
        string jsonData = JsonUtility.ToJson(data);
        // Create a HTTP request
        using(HttpClient client = new HttpClient()) 
        {   
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, flaskServer);
            requestMessage.Content = new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json");
            requestMessage.Headers.Add("User-Agent", "UnityHttpClient/1.0");

            // Establish a POST request to the Flask server link
            HttpResponseMessage response = await client.SendAsync(requestMessage);
            // Exception handling
            response.EnsureSuccessStatusCode();
            
            // Return the response from the backend
            string jsonResponse = await response.Content.ReadAsStringAsync();
            Debug.Log("Response received: " + jsonResponse);
            ResponseData result = JsonUtility.FromJson<ResponseData>(jsonResponse);
            return result.saved_mesh;
        }
    }

    async Task<byte[]> DownloadObjFile(string file_path)
    {
        using (HttpClient client = new HttpClient())
        {
            string processedPath = Path.GetFileName(file_path);
            Debug.Log(processedPath);
            string url = $"http://127.0.0.1:5001/download_mesh/{processedPath}";
            Debug.Log(url);
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync();
        }
    }

    Mesh LoadMeshFromObj(byte[] objFile)
    {
        string objFileText = System.Text.Encoding.UTF8.GetString(objFile);
        Mesh mesh = new Mesh();

        List<Vector3> vertices = new List<Vector3>();
        List<int> faces = new List<int>();

        string[] lines = objFileText.Split('\n');
        foreach (string line in lines)
        {
            if (line.StartsWith("v "))
            {
                string[] split = line.Substring(2).Split(' ');
                Vector3 vertex = new Vector3(
                    float.Parse(split[0]),
                    float.Parse(split[1]),
                    float.Parse(split[2])
                );
                vertices.Add(vertex);
            }
            else if (line.StartsWith("f "))
            {
                string[] split = line.Substring(2).Split(' ');
                for (int i = 0; i < split.Length; i++) 
                {
                    int vertexIndex = int.Parse(split[i]) - 1;
                    faces.Add(vertexIndex);
                }
            }
        }
        mesh.vertices = vertices.ToArray();
        mesh.triangles = faces.ToArray();

        mesh.RecalculateNormals();
        Debug.Log($"Loaded mesh with {mesh.vertexCount} vertices and {mesh.triangles.Length / 3} triangles.");

        return mesh;
    }

    void ApplyMeshToModel(Mesh newMesh)
    {
        MeshFilter meshFilter = headModel.GetComponentInChildren<MeshFilter>();
        if (meshFilter != null)
        {
            Debug.Log($"Applying new mesh with {newMesh.vertexCount} vertices to the model.");
            meshFilter.mesh = newMesh;
        }
        else
        {
            Debug.LogError("MeshFilter component not found on headModel.");
        }
    }
}

