using UnityEngine;

namespace FunClass.Core
{
    [RequireComponent(typeof(CharacterController))]
    public class TeacherController : MonoBehaviour
    {
        public static TeacherController Instance { get; private set; }

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float gravity = -9.81f;

        [Header("Camera")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float lookSensitivity = 2f;
        [SerializeField] private float maxLookAngle = 80f;

        [Header("Item Holding")]
        [SerializeField] private Transform itemHoldPoint;

        [Header("Interaction")]
        [SerializeField] private float interactionRange = 3f;
        [SerializeField] private LayerMask interactionLayer = ~0;

        private CharacterController characterController;
        private HeldItem currentHeldItem;
        private float verticalVelocity;
        private float cameraPitch;
        private bool isActive = false;
        private InteractableObject currentLookTarget;
        private StudentAgent currentStudentTarget;

        public Camera PlayerCamera => playerCamera;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            characterController = GetComponent<CharacterController>();

            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }

            if (cameraTransform == null && playerCamera != null)
            {
                cameraTransform = playerCamera.transform;
            }
        }

        void OnEnable()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged += HandleGameStateChanged;
            }
        }

        void OnDisable()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged -= HandleGameStateChanged;
            }
        }

        void Start()
        {
            if (GameStateManager.Instance != null)
            {
                HandleGameStateChanged(GameStateManager.Instance.CurrentState, GameStateManager.Instance.CurrentState);
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Update()
        {
            if (!isActive) return;

            HandleMovement();
            HandleCamera();
            HandleInteractionDetection();
            HandleInteractionInput();
        }

        private void HandleGameStateChanged(GameState oldState, GameState newState)
        {
            if (newState == GameState.InLevel)
            {
                ActivateTeacher();
            }
            else
            {
                DeactivateTeacher();
            }
        }

        private void ActivateTeacher()
        {
            isActive = true;
            enabled = true;
            Debug.Log("[TeacherController] Teacher activated");
        }

        private void DeactivateTeacher()
        {
            isActive = false;
            enabled = false;
            Debug.Log("[TeacherController] Teacher deactivated");
        }

        private void HandleMovement()
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            Vector3 moveDirection = transform.right * horizontal + transform.forward * vertical;
            moveDirection.Normalize();

            Vector3 move = moveDirection * moveSpeed;

            if (characterController.isGrounded)
            {
                verticalVelocity = -2f;
            }
            else
            {
                verticalVelocity += gravity * Time.deltaTime;
            }

            move.y = verticalVelocity;

            characterController.Move(move * Time.deltaTime);
        }

        private void HandleCamera()
        {
            if (cameraTransform == null) return;

            float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;

            transform.Rotate(Vector3.up * mouseX);

            cameraPitch -= mouseY;
            cameraPitch = Mathf.Clamp(cameraPitch, -maxLookAngle, maxLookAngle);
            cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }

        public void HoldItem(HeldItem item)
        {
            if (currentHeldItem != null)
            {
                DropItem();
            }

            currentHeldItem = item;

            if (itemHoldPoint != null && item != null)
            {
                item.transform.SetParent(itemHoldPoint);
                item.transform.localPosition = Vector3.zero;
                item.transform.localRotation = Quaternion.identity;
            }

            Debug.Log($"[TeacherController] Now holding: {item?.itemName ?? "nothing"}");
        }

        public void DropItem()
        {
            if (currentHeldItem == null) return;

            Debug.Log($"[TeacherController] Dropped: {currentHeldItem.itemName}");

            currentHeldItem.transform.SetParent(null);
            currentHeldItem = null;
        }

        public HeldItem GetHeldItem()
        {
            return currentHeldItem;
        }

        public bool IsHoldingItem()
        {
            return currentHeldItem != null;
        }

        private void HandleInteractionDetection()
        {
            if (cameraTransform == null) return;

            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, interactionRange, interactionLayer))
            {
                StudentAgent student = hit.collider.GetComponentInParent<StudentAgent>();
                
                if (student != null)
                {
                    if (student != currentStudentTarget)
                    {
                        currentStudentTarget = student;
                        currentLookTarget = null;
                        
                        Debug.Log($"[TeacherController] Looking at: {currentStudentTarget.GetInteractionPrompt()}");
                    }
                }
                else
                {
                    InteractableObject interactable = hit.collider.GetComponent<InteractableObject>();
                    
                    if (interactable != currentLookTarget)
                    {
                        currentLookTarget = interactable;
                        currentStudentTarget = null;
                        
                        if (currentLookTarget != null)
                        {
                            Debug.Log($"[TeacherController] Looking at: {currentLookTarget.GetInteractionPrompt()}");
                        }
                    }
                }
            }
            else
            {
                if (currentLookTarget != null)
                {
                    currentLookTarget = null;
                }
                if (currentStudentTarget != null)
                {
                    currentStudentTarget = null;
                }
            }
        }

        private void HandleInteractionInput()
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (currentStudentTarget != null)
                {
                    InteractWithStudent(currentStudentTarget);
                }
                else if (currentLookTarget != null)
                {
                    currentLookTarget.Interact(this);
                }
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                if (currentStudentTarget != null)
                {
                    currentStudentTarget.HandleTeacherAction(TeacherActionType.Calm);
                }
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                if (currentStudentTarget != null)
                {
                    currentStudentTarget.HandleTeacherAction(TeacherActionType.SendToSeat);
                }
            }

            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                if (currentStudentTarget != null)
                {
                    currentStudentTarget.HandleTeacherAction(TeacherActionType.Stop);
                }
            }

            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                if (currentStudentTarget != null && currentHeldItem != null)
                {
                    UseItemOnStudent(currentStudentTarget, currentHeldItem.gameObject);
                }
            }

            if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                if (currentStudentTarget != null)
                {
                    currentStudentTarget.HandleTeacherAction(TeacherActionType.Scold);
                }
            }

            if (Input.GetKeyDown(KeyCode.Alpha6))
            {
                if (currentStudentTarget != null)
                {
                    currentStudentTarget.HandleTeacherAction(TeacherActionType.Talk);
                }
            }

            if (Input.GetKeyDown(KeyCode.Alpha7))
            {
                if (currentStudentTarget != null)
                {
                    currentStudentTarget.HandleTeacherAction(TeacherActionType.Praise);
                }
            }
        }

        public InteractableObject GetCurrentLookTarget()
        {
            return currentLookTarget;
        }

        public StudentAgent GetCurrentStudentTarget()
        {
            return currentStudentTarget;
        }

        private void InteractWithStudent(StudentAgent student)
        {
            if (student == null) return;

            student.InteractWithTeacher(this);

            TeacherActionType action = student.CurrentState switch
            {
                StudentState.Calm => TeacherActionType.Talk,
                StudentState.Distracted => TeacherActionType.Calm,
                StudentState.ActingOut => TeacherActionType.Stop,
                StudentState.Critical => TeacherActionType.SendToSeat,
                _ => TeacherActionType.Talk
            };

            student.HandleTeacherAction(action);
        }

        private void UseItemOnStudent(StudentAgent student, GameObject item)
        {
            if (student == null || item == null) return;

            Debug.Log($"[TeacherController] Using {item.name} on {student.Config?.studentName}");

            HeldItem heldItem = item.GetComponent<HeldItem>();
            if (heldItem != null && heldItem.itemName.ToLower().Contains("whistle"))
            {
                student.StopCurrentAction();
                student.CalmDown();
            }
            else
            {
                student.TakeObjectAway(item);
            }
        }
    }
}
