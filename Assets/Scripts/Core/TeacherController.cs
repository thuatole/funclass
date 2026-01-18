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
                        
                        string prompt = GetContextualPrompt(student);
                        Debug.Log($"[TeacherController] Looking at: {prompt}");
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
                    HandleContextualInteraction(currentStudentTarget);
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

        /// <summary>
        /// Determines if a student is outside the classroom based on LevelConfig locations
        /// </summary>
        private bool IsStudentOutsideClassroom(StudentAgent student)
        {
            if (student == null || LevelManager.Instance == null) return false;

            LevelConfig currentLevel = LevelManager.Instance.GetCurrentLevelConfig();
            if (currentLevel == null || currentLevel.classroomDoor == null) return false;

            // Check if student is beyond the classroom door
            Vector3 doorPosition = currentLevel.classroomDoor.position;
            Vector3 studentPosition = student.transform.position;
            
            // Simple distance check - can be made more sophisticated
            float distanceFromDoor = Vector3.Distance(studentPosition, doorPosition);
            
            // If student is far from their seat and near/past the door, they're outside
            float distanceFromSeat = Vector3.Distance(studentPosition, student.OriginalSeatPosition);
            
            return distanceFromSeat > 5f && distanceFromDoor < 10f;
        }

        /// <summary>
        /// Gets a context-sensitive interaction prompt based on student location and state
        /// </summary>
        private string GetContextualPrompt(StudentAgent student)
        {
            if (student == null || student.Config == null) return "Interact with student";

            string studentName = student.Config.studentName;
            
            // Check if student is outside classroom
            bool isOutside = IsStudentOutsideClassroom(student);
            
            // Check if student is following a route
            bool isOnRoute = student.IsFollowingRoute;
            StudentRoute currentRoute = student.GetCurrentRoute();

            if (isOutside || isOnRoute)
            {
                // Student is outside or escaping
                if (student.CurrentState == StudentState.Critical)
                {
                    return $"Press E to escort {studentName} back to class";
                }
                else
                {
                    return $"Press E to call {studentName} back to class";
                }
            }
            else
            {
                // Normal classroom interaction
                return student.CurrentState switch
                {
                    StudentState.Calm => $"Talk to {studentName}",
                    StudentState.Distracted => $"Calm down {studentName}",
                    StudentState.ActingOut => $"Stop {studentName}",
                    StudentState.Critical => $"Send {studentName} back to seat",
                    _ => $"Interact with {studentName}"
                };
            }
        }

        /// <summary>
        /// Handles context-sensitive interaction based on student location and state
        /// </summary>
        private void HandleContextualInteraction(StudentAgent student)
        {
            if (student == null) return;

            bool isOutside = IsStudentOutsideClassroom(student);
            bool isOnRoute = student.IsFollowingRoute;

            if (isOutside || isOnRoute)
            {
                // Student is outside or escaping - use recall actions
                if (student.CurrentState == StudentState.Critical)
                {
                    EscortStudentBack(student);
                }
                else
                {
                    CallStudentBack(student);
                }
            }
            else
            {
                // Normal classroom interaction
                InteractWithStudent(student);
            }
        }

        /// <summary>
        /// Calls a student back to class using the return route
        /// </summary>
        private void CallStudentBack(StudentAgent student)
        {
            if (student == null) return;

            Debug.Log($"[Teacher] Called {student.Config?.studentName} back to class");

            // Stop current route/escape behavior
            student.StopRoute();

            // Get return route from level config
            if (LevelManager.Instance != null)
            {
                LevelConfig currentLevel = LevelManager.Instance.GetCurrentLevelConfig();
                if (currentLevel != null && currentLevel.returnRoute != null)
                {
                    student.StartRoute(currentLevel.returnRoute);
                    Debug.Log($"[Teacher] {student.Config?.studentName} starting return route");
                }
                else
                {
                    // Fallback: direct return to seat
                    student.ReturnToSeat();
                }
            }
            else
            {
                student.ReturnToSeat();
            }

            // Trigger teacher action
            student.HandleTeacherAction(TeacherActionType.CallStudentBack);

            // Reduce classroom disruption
            if (ClassroomManager.Instance != null)
            {
                ClassroomManager.Instance.AddDisruption(-10f, $"{student.Config?.studentName} called back");
            }

            // Log event
            if (StudentEventManager.Instance != null)
            {
                StudentEventManager.Instance.LogEvent(
                    student,
                    StudentEventType.StudentCalmed,
                    "was called back to class by teacher"
                );
            }
        }

        /// <summary>
        /// Escorts a critical student back to their seat
        /// </summary>
        private void EscortStudentBack(StudentAgent student)
        {
            if (student == null) return;

            Debug.Log($"[Teacher] Escorting {student.Config?.studentName} back to seat");

            // Stop current behavior
            student.StopRoute();

            // Force immediate return
            student.ReturnToSeat();

            // Trigger teacher action
            student.HandleTeacherAction(TeacherActionType.EscortStudentBack);

            // Significant disruption reduction
            if (ClassroomManager.Instance != null)
            {
                ClassroomManager.Instance.AddDisruption(-15f, $"{student.Config?.studentName} escorted back");
            }

            // Log event
            if (StudentEventManager.Instance != null)
            {
                StudentEventManager.Instance.LogEvent(
                    student,
                    StudentEventType.StudentReturnedToSeat,
                    "was escorted back to seat by teacher"
                );
            }
        }

        /// <summary>
        /// Forces a student to return to their seat (can be called via input)
        /// </summary>
        public void ForceStudentReturnToSeat(StudentAgent student)
        {
            if (student == null) return;

            Debug.Log($"[Teacher] Forcing {student.Config?.studentName} to return to seat");

            student.StopRoute();
            student.ReturnToSeat();
            student.HandleTeacherAction(TeacherActionType.ForceReturnToSeat);

            if (ClassroomManager.Instance != null)
            {
                ClassroomManager.Instance.AddDisruption(-5f, $"{student.Config?.studentName} forced to seat");
            }
        }
    }
}
