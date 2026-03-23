export function init(container, dotNetHelper) {
    class ProfileImageCropper {
        constructor() {
            this.dotNetHelper = dotNetHelper;
            this.container = container;

            // Warten bis DOM vollständig geladen ist
            this.initializeElements();

            // Status Variablen
            this.image = null;
            this.isDragging = false;
            this.isResizing = false;
            this.cropCircle = { x: 175, y: 175, radius: 80 };

            // Canvas Konfiguration
            this.canvasWidth = 350;
            this.canvasHeight = 350;
            this.scale = 1;
            this.imageOffset = { x: 0, y: 0 };
            this.dragStart = { x: 0, y: 0 };
            this.minRadius = 30;
            this.maxRadius = 150;

            this.initEventListeners();
        }

        initializeElements() {
            // Elemente suchen - innerhalb des Containers
            this.canvas = this.container.querySelector('#crop-canvas');
            this.ctx = this.canvas ? this.canvas.getContext('2d') : null;
            this.dialog = this.container.querySelector('#crop-dialog');

            console.log('Initialized elements:', {
                canvas: !!this.canvas,
                dialog: !!this.dialog,
                container: !!this.container
            });
        }

        initEventListeners() {
            // Event Delegation für Buttons
            this.container.addEventListener('click', (e) => {
                if (e.target.closest('#change-profile-btn')) {
                    e.preventDefault();
                    console.log('Change button clicked');
                    const fileInput = this.container.querySelector('#profile-file-input');
                    if (fileInput) {
                        fileInput.click();
                    } else {
                        console.error('File input not found');
                    }
                }

                if (e.target.closest('#remove-profile-btn')) {
                    e.preventDefault();
                    this.removeProfileImage();
                }

                if (e.target.closest('#crop-apply-btn')) {
                    e.preventDefault();
                    this.applyCrop();
                }

                if (e.target.closest('#crop-cancel-btn')) {
                    e.preventDefault();
                    this.closeCropDialog();
                }
            });

            // File Input Handler mit MutationObserver
            this.bindFileInput();

            const observer = new MutationObserver(() => {
                this.bindFileInput();
                // Re-initialisiere Elemente falls sie neu gerendert wurden
                if (!this.dialog || !this.canvas) {
                    this.initializeElements();
                }
            });

            observer.observe(this.container, {
                childList: true,
                subtree: true
            });

            // Canvas Events
            if (this.canvas) {
                this.canvas.addEventListener('mousedown', (e) => this.startCrop(e));
                window.addEventListener('mousemove', (e) => {
                    if(this.isDragging || this.isResizing) this.updateCrop(e)
                });
                window.addEventListener('mouseup', () => this.endCrop());

                // Touch Events
                this.canvas.addEventListener('touchstart', (e) => {
                    e.preventDefault();
                    const touch = e.touches[0];
                    const mouseEvent = new MouseEvent('mousedown', {
                        clientX: touch.clientX,
                        clientY: touch.clientY
                    });
                    this.startCrop(mouseEvent);
                }, { passive: false });

                this.canvas.addEventListener('touchmove', (e) => {
                    e.preventDefault();
                    const touch = e.touches[0];
                    const mouseEvent = new MouseEvent('mousemove', {
                        clientX: touch.clientX,
                        clientY: touch.clientY
                    });
                    this.updateCrop(mouseEvent);
                }, { passive: false });

                this.canvas.addEventListener('touchend', (e) => {
                    e.preventDefault();
                    this.endCrop();
                }, { passive: false });
            }
        }

        bindFileInput() {
            const fileInput = this.container.querySelector('#profile-file-input');
            if (fileInput && !fileInput.dataset.bound) {
                fileInput.dataset.bound = 'true';
                fileInput.addEventListener('change', (e) => {
                    console.log('File selected:', e.target.files);
                    if (e.target.files && e.target.files[0]) {
                        this.loadImage(e.target.files[0]);
                    }
                });
            }
        }

        loadImage(file) {
            console.log('Loading image:', file.name, file.type, file.size);

            if (!file.type.match(/^image\/(jpeg|png|gif)$/)) {
                alert('Bitte wählen Sie eine gültige Bilddatei (JPG, PNG oder GIF).');
                return;
            }

            if (file.size > 5 * 1024 * 1024) { // 5MB Limit erhöht
                alert('Die Datei ist zu groß. Maximale Größe: 5MB.');
                return;
            }

            const reader = new FileReader();
            reader.onload = (e) => {
                this.image = new Image();
                this.image.onload = () => {
                    console.log('Image loaded, opening dialog');
                    this.setupCanvas();
                    this.openCropDialog();
                };
                this.image.onerror = () => {
                    console.error('Failed to load image');
                    alert('Fehler beim Laden des Bildes.');
                };
                this.image.src = e.target.result;
            };
            reader.onerror = () => {
                console.error('FileReader error');
                alert('Fehler beim Lesen der Datei.');
            };
            reader.readAsDataURL(file);
        }

        setupCanvas() {
            if (!this.canvas) {
                console.error('Canvas not found');
                return;
            }

            const maxWidth = this.canvasWidth;
            const maxHeight = this.canvasHeight;
            let { width, height } = this.image;

            if (width > height) {
                if (width > maxWidth) {
                    height = (height * maxWidth) / width;
                    width = maxWidth;
                }
            } else {
                if (height > maxHeight) {
                    width = (width * maxHeight) / height;
                    height = maxHeight;
                }
            }

            this.scale = width / this.image.width;
            this.imageOffset.x = (maxWidth - width) / 2;
            this.imageOffset.y = (maxHeight - height) / 2;

            this.cropCircle = {
                x: maxWidth / 2,
                y: maxHeight / 2,
                radius: Math.min(80, Math.min(width, height) / 4)
            };

            this.redrawCanvas();
        }

        redrawCanvas() {
            if (!this.ctx) return;

            this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);

            if (this.image) {
                const scaledWidth = this.image.width * this.scale;
                const scaledHeight = this.image.height * this.scale;

                this.ctx.drawImage(
                    this.image,
                    this.imageOffset.x,
                    this.imageOffset.y,
                    scaledWidth,
                    scaledHeight
                );
            }

            this.ctx.fillStyle = 'rgba(0, 0, 0, 0.6)';
            this.ctx.fillRect(0, 0, this.canvas.width, this.canvas.height);

            this.ctx.globalCompositeOperation = 'destination-out';
            this.ctx.beginPath();
            this.ctx.arc(this.cropCircle.x, this.cropCircle.y, this.cropCircle.radius, 0, 2 * Math.PI);
            this.ctx.fill();

            this.ctx.globalCompositeOperation = 'source-over';
            this.ctx.strokeStyle = '#0ea5e9';
            this.ctx.lineWidth = 2;
            this.ctx.beginPath();
            this.ctx.arc(this.cropCircle.x, this.cropCircle.y, this.cropCircle.radius, 0, 2 * Math.PI);
            this.ctx.stroke();

            this.ctx.fillStyle = '#0ea5e9';
            this.ctx.beginPath();
            this.ctx.arc(
                this.cropCircle.x + this.cropCircle.radius,
                this.cropCircle.y,
                6,
                0,
                2 * Math.PI
            );
            this.ctx.fill();
        }

        getMousePos(e) {
            const rect = this.canvas.getBoundingClientRect();
            return {
                x: e.clientX - rect.left,
                y: e.clientY - rect.top
            };
        }

        getDistance(x1, y1, x2, y2) {
            return Math.sqrt(Math.pow(x2 - x1, 2) + Math.pow(y2 - y1, 2));
        }

        isPointInCircle(x, y, centerX, centerY, radius) {
            return this.getDistance(x, y, centerX, centerY) <= radius;
        }

        isPointNearEdge(x, y, centerX, centerY, radius, tolerance = 8) {
            const distance = this.getDistance(x, y, centerX, centerY);
            return Math.abs(distance - radius) <= tolerance;
        }

        startCrop(e) {
            const pos = this.getMousePos(e);
            const resizeHandleX = this.cropCircle.x + this.cropCircle.radius;
            const resizeHandleY = this.cropCircle.y;

            if (this.getDistance(pos.x, pos.y, resizeHandleX, resizeHandleY) <= 15) {
                this.isResizing = true;
                this.canvas.style.cursor = 'nw-resize';
                return;
            }

            if (this.isPointNearEdge(pos.x, pos.y, this.cropCircle.x, this.cropCircle.y, this.cropCircle.radius)) {
                this.isResizing = true;
                this.canvas.style.cursor = 'nw-resize';
                return;
            }

            if (this.isPointInCircle(pos.x, pos.y, this.cropCircle.x, this.cropCircle.y, this.cropCircle.radius)) {
                this.isDragging = true;
                this.dragStart = {
                    x: pos.x - this.cropCircle.x,
                    y: pos.y - this.cropCircle.y
                };
                this.canvas.style.cursor = 'move';
            }
        }

        updateCrop(e) {
            const pos = this.getMousePos(e);

            if (this.isResizing) {
                const newRadius = this.getDistance(pos.x, pos.y, this.cropCircle.x, this.cropCircle.y);
                this.cropCircle.radius = Math.max(this.minRadius, Math.min(this.maxRadius, newRadius));
                this.redrawCanvas();
                return;
            }

            if (this.isDragging) {
                let newX = pos.x - this.dragStart.x;
                let newY = pos.y - this.dragStart.y;

                newX = Math.max(this.cropCircle.radius, Math.min(newX, this.canvas.width - this.cropCircle.radius));
                newY = Math.max(this.cropCircle.radius, Math.min(newY, this.canvas.height - this.cropCircle.radius));

                this.cropCircle.x = newX;
                this.cropCircle.y = newY;
                this.redrawCanvas();
                return;
            }

            const resizeHandleX = this.cropCircle.x + this.cropCircle.radius;
            const resizeHandleY = this.cropCircle.y;
            if (this.getDistance(pos.x, pos.y, resizeHandleX, resizeHandleY) <= 15 ||
                this.isPointNearEdge(pos.x, pos.y, this.cropCircle.x, this.cropCircle.y, this.cropCircle.radius)) {
                this.canvas.style.cursor = 'nw-resize';
            } else if (this.isPointInCircle(pos.x, pos.y, this.cropCircle.x, this.cropCircle.y, this.cropCircle.radius)) {
                this.canvas.style.cursor = 'move';
            } else {
                this.canvas.style.cursor = 'default';
            }
        }

        endCrop() {
            this.isDragging = false;
            this.isResizing = false;
        }

        applyCrop() {
            if (!this.image || !this.ctx) return;

            const outputSize = this.cropCircle.radius * 2;
            const outputCanvas = document.createElement('canvas');
            const outputCtx = outputCanvas.getContext('2d');
            outputCanvas.width = outputSize;
            outputCanvas.height = outputSize;

            const sourceX = (this.cropCircle.x - this.cropCircle.radius - this.imageOffset.x) / this.scale;
            const sourceY = (this.cropCircle.y - this.cropCircle.radius - this.imageOffset.y) / this.scale;
            const sourceSize = (this.cropCircle.radius * 2) / this.scale;

            outputCtx.save();
            outputCtx.beginPath();
            outputCtx.arc(outputSize / 2, outputSize / 2, outputSize / 2, 0, 2 * Math.PI);
            outputCtx.closePath();
            outputCtx.clip();

            outputCtx.drawImage(this.image, sourceX, sourceY, sourceSize, sourceSize, 0, 0, outputSize, outputSize);
            outputCtx.restore();

            const base64 = outputCanvas.toDataURL('image/png');

            const profileImg = this.container.querySelector('#profile-image');
            if(profileImg) profileImg.src = base64;

            if(this.dotNetHelper) {
                this.dotNetHelper.invokeMethodAsync('UpdateProfileImage', base64);
            }

            this.closeCropDialog();
        }

        removeProfileImage() {
            if (confirm('Sind Sie sicher, dass Sie Ihr Profilbild entfernen möchten?')) {
                const defaultImg = 'https://api.dicebear.com/7.x/avataaars/svg?seed';
                const profileImg = this.container.querySelector('#profile-image');
                if(profileImg) profileImg.src = defaultImg;

                if(this.dotNetHelper) {
                    this.dotNetHelper.invokeMethodAsync('RemoveProfileImage');
                }
            }
        }

        openCropDialog() {
            console.log('Opening crop dialog, dialog element:', this.dialog);
            if(this.dialog) {
                this.dialog.classList.remove('auth-hidden');
                document.body.style.overflow = 'hidden';
                console.log('Dialog opened');
            } else {
                console.error('Dialog element not found!');
            }
        }

        closeCropDialog() {
            console.log('Closing crop dialog');
            if(this.dialog) {
                this.dialog.classList.add('auth-hidden');
                document.body.style.overflow = '';
                const input = this.container.querySelector('#profile-file-input');
                if(input) input.value = '';
                console.log('Dialog closed');
            }
        }
    }

    new ProfileImageCropper();
}