// // wwwroot/js/profile-edit.js

// // Counter for dynamic items
// let educationCount = @Model.Education.Count;
// let experienceCount = @Model.Experience.Count;
// let projectCount = @Model.Projects.Count;
// let certificationCount = @Model.Certifications.Count;
// let achievementCount = @Model.Achievements.Count;

// function addEducation() {
//     const template = `
//         <div class="education-item">
//             <h4>Education ${++educationCount}</h4>
//             <div class="form-row">
//                 <div class="form-group">
//                     <label>Degree</label>
//                     <input type="text" name="Education[${educationCount-1}].Degree" class="form-control" />
//                 </div>
//                 <div class="form-group">
//                     <label>Institution</label>
//                     <input type="text" name="Education[${educationCount-1}].Institution" class="form-control" />
//                 </div>
//             </div>
//             <div class="form-row">
//                 <div class="form-group">
//                     <label>Duration</label>
//                     <input type="text" name="Education[${educationCount-1}].Duration" class="form-control" placeholder="e.g., 2020-2024" />
//                 </div>
//             </div>
//             <div class="form-group">
//                 <label>Highlights (one per line)</label>
//                 <textarea name="Education[${educationCount-1}].HighlightsText" class="form-control" rows="3"></textarea>
//             </div>
//             <button type="button" class="btn-danger remove-item" onclick="removeItem(this)">Remove</button>
//             <hr />
//         </div>
//     `;
    
//     document.getElementById('education-list').insertAdjacentHTML('beforeend', template);
// }

// function addExperience() {
//     const template = `
//         <div class="experience-item">
//             <h4>Experience ${++experienceCount}</h4>
//             <div class="form-row">
//                 <div class="form-group">
//                     <label>Company</label>
//                     <input type="text" name="Experience[${experienceCount-1}].Company" class="form-control" />
//                 </div>
//                 <div class="form-group">
//                     <label>Role</label>
//                     <input type="text" name="Experience[${experienceCount-1}].Role" class="form-control" />
//                 </div>
//             </div>
//             <div class="form-row">
//                 <div class="form-group">
//                     <label>Location</label>
//                     <input type="text" name="Experience[${experienceCount-1}].Location" class="form-control" />
//                 </div>
//                 <div class="form-group">
//                     <label>Duration</label>
//                     <input type="text" name="Experience[${experienceCount-1}].Duration" class="form-control" />
//                 </div>
//             </div>
//             <div class="form-group">
//                 <label>Highlights (one per line)</label>
//                 <textarea name="Experience[${experienceCount-1}].HighlightsText" class="form-control" rows="3"></textarea>
//             </div>
//             <button type="button" class="btn-danger remove-item" onclick="removeItem(this)">Remove</button>
//             <hr />
//         </div>
//     `;
    
//     document.getElementById('experience-list').insertAdjacentHTML('beforeend', template);
// }

// function addProject() {
//     const template = `
//         <div class="project-item">
//             <h4>Project ${++projectCount}</h4>
//             <div class="form-row">
//                 <div class="form-group">
//                     <label>Title</label>
//                     <input type="text" name="Projects[${projectCount-1}].Title" class="form-control" />
//                 </div>
//                 <div class="form-group">
//                     <label>Duration</label>
//                     <input type="text" name="Projects[${projectCount-1}].Duration" class="form-control" />
//                 </div>
//             </div>
//             <div class="form-group">
//                 <label>Highlights (one per line)</label>
//                 <textarea name="Projects[${projectCount-1}].HighlightsText" class="form-control" rows="3"></textarea>
//             </div>
//             <button type="button" class="btn-danger remove-item" onclick="removeItem(this)">Remove</button>
//             <hr />
//         </div>
//     `;
    
//     document.getElementById('projects-list').insertAdjacentHTML('beforeend', template);
// }

// function addCertification() {
//     const template = `
//         <div class="certification-item">
//             <input type="text" name="Certifications[${certificationCount}]" class="form-control" placeholder="Certification name" />
//             <button type="button" class="btn-small btn-danger" onclick="this.parentElement.remove()">×</button>
//         </div>
//     `;
    
//     document.getElementById('certifications-list').insertAdjacentHTML('beforeend', template);
//     certificationCount++;
// }

// function addAchievement() {
//     const template = `
//         <div class="achievement-item">
//             <input type="text" name="Achievements[${achievementCount}]" class="form-control" placeholder="Achievement description" />
//             <button type="button" class="btn-small btn-danger" onclick="this.parentElement.remove()">×</button>
//         </div>
//     `;
    
//     document.getElementById('achievements-list').insertAdjacentHTML('beforeend', template);
//     achievementCount++;
// }

// function removeItem(button) {
//     if (confirm('Are you sure you want to remove this item?')) {
//         button.closest('.education-item, .experience-item, .project-item').remove();
//     }
// }

// // Handle form submission - convert textareas to arrays
// document.getElementById('profileForm').addEventListener('submit', function(e) {
//     // Convert highlights textareas to arrays
//     document.querySelectorAll('.highlights-textarea').forEach(textarea => {
//         const lines = textarea.value.split('\n').filter(line => line.trim() !== '');
//         const hiddenInput = document.createElement('input');
//         hiddenInput.type = 'hidden';
//         hiddenInput.name = textarea.name;
//         hiddenInput.value = JSON.stringify(lines);
//         textarea.parentNode.appendChild(hiddenInput);
//         textarea.disabled = true; // Don't submit the textarea
//     });
// });