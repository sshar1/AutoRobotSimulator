using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldMapManager : MonoBehaviour {

    [SerializeField] private Transform robot;
    [SerializeField] private GameObject robotNode;

    [SerializeField] private List<Cargo> cargos;
    private Dictionary<Cargo, GameObject> cargoNodes;

    [SerializeField] private GameObject sampleRedCargoNode;
    [SerializeField] private GameObject sampleBlueCargoNode;

    private const float FIELD_WIDTH = 24.69f;
    private const float FIELD_HEIGHT = 49.35f;

    private const float MAP_WIDTH = 246.9f;
    private const float MAP_HEIGHT = 493.5f;

    private void Start() {
        cargoNodes = new Dictionary<Cargo, GameObject>();

        foreach (Cargo cargo in cargos) {
            if (cargo.GetColor() == Cargo.Color.RED) {
                GameObject cargoNode = Instantiate(sampleRedCargoNode, sampleRedCargoNode.transform.parent);
                cargoNode.SetActive(true);
                cargoNodes.Add(cargo, cargoNode);
            } else if (cargo.GetColor() == Cargo.Color.BLUE) {
                GameObject cargoNode = Instantiate(sampleBlueCargoNode, sampleBlueCargoNode.transform.parent);
                cargoNode.SetActive(true);
                cargoNodes.Add(cargo, cargoNode);
            }
        }
    }

    private void Update() {
        Vector3 robotPosition = robot.position;

        robotNode.transform.localPosition = new Vector3(robotPosition.x * MAP_WIDTH / FIELD_WIDTH, robotPosition.z * MAP_HEIGHT / FIELD_HEIGHT, 0);
        robotNode.transform.rotation = Quaternion.Euler(0, 0, -robot.GetComponent<Drivetrain>().GetHeading());

        foreach (KeyValuePair<Cargo, GameObject> pair in cargoNodes) {
            Vector3 cargoPosition = pair.Key.transform.position;

            if (cargoPosition.y > 1f || pair.Key.GetComponent<Cargo>().IsInRobot()) {
                pair.Value.SetActive(false);
                continue;
            } else {
                pair.Value.SetActive(true);
            }

            pair.Value.transform.localPosition = new Vector3(cargoPosition.x * MAP_WIDTH / FIELD_WIDTH, cargoPosition.z * MAP_HEIGHT / FIELD_HEIGHT, 0);
        }
    }
}
