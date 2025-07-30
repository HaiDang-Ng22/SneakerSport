
        /* ---------- Helpers ---------- */
        function showToast(msg,type='info'){
            const zone=document.getElementById('toastZone');
            const div=document.createElement('div');
            div.className='toast-msg '+(type==='error'?'error':type==='success'?'success':'');
            div.textContent=msg;
            zone.appendChild(div);
            setTimeout(()=>{div.style.opacity='0';setTimeout(()=>div.remove(),400)},4000);
        }
        function validateEmailFormat(e){
            // regex chỉ 1 @
            return /^[\\w.+-]+@[\\w-]+\\.[\\w.-]{2,}$/i.test(e);
        }
        async function postJson(url,data, includeAnti = false){
            const headers = {
                'Content-Type':'application/json',
                'X-Requested-With':'XMLHttpRequest'
            };
            if(includeAnti){
                const token = document.querySelector('#finalForm input[name="__RequestVerificationToken"]');
                if(token) headers['RequestVerificationToken'] = token.value;
            }
            const res = await fetch(url,{
                method:'POST',
                headers,
                body: JSON.stringify(data)
            });
            // Trường hợp bị redirect (401 -> login) trả về HTML
            const ct = res.headers.get('content-type') || '';
            if(!ct.includes('application/json')){
                const text = await res.text();
                throw new Error('Phản hồi không phải JSON (có thể thiếu AllowAnonymous / bị redirect).');
            }
            return await res.json();
        }

        /* ---------- DOM refs ---------- */
        const step1 = document.getElementById('step1Section');
        const step2 = document.getElementById('step2Section');
        const step3 = document.getElementById('step3Section');
        const step4 = document.getElementById('step4Section');
        const ind1 = document.getElementById('step1Indicator');
        const ind2 = document.getElementById('step2Indicator');
        const ind3 = document.getElementById('step3Indicator');

        const emailInput = document.getElementById('email');
        const emailFeedback = document.getElementById('emailFeedback');
        const emailStatus = document.getElementById('emailStatus');
        const checkEmailBtn = document.getElementById('checkEmailBtn');

        const userEmailEl = document.getElementById('userEmail');
        const resendBtn = document.getElementById('resendOtpBtn');
        const verifyOtpBtn = document.getElementById('verifyOtpBtn');
        const backToEmailBtn = document.getElementById('backToEmailBtn');
        const backToOtpBtn = document.getElementById('backToOtpBtn');
        const otpFeedback = document.getElementById('otpFeedback');
        const countdownEl = document.getElementById('countdown');
        const otpInputs = document.querySelectorAll('.otp-input');

        const finalEmail = document.getElementById('finalEmail');
        const hoTenInput = document.getElementById('hoTen');
        const tenDangNhapInput = document.getElementById('tenDangNhap');
        const passwordInput = document.getElementById('password');
        const confirmPasswordInput = document.getElementById('confirmPassword');
        const soDienThoaiInput = document.getElementById('soDienThoai');
        const diaChiInput = document.getElementById('diaChi');
        const termsCheckbox = document.getElementById('terms');
        const submitRegisterBtn = document.getElementById('submitRegisterBtn');
        const finalForm = document.getElementById('finalForm');

        let currentEmail = '';
        let cooldown = 0;
        let timer = null;
        let otpVerified = false;

        function showStep(n){
            [step1,step2,step3,step4].forEach(s=>s.classList.add('hidden-section'));
            [ind1,ind2,ind3].forEach(i=>i.classList.remove('active'));
            if(n===1){ step1.classList.remove('hidden-section'); ind1.classList.add('active'); }
            if(n===2){ step2.classList.remove('hidden-section'); ind2.classList.add('active'); }
            if(n===3){ step3.classList.remove('hidden-section'); ind3.classList.add('active'); }
            if(n===4){ step4.classList.remove('hidden-section'); }
        }
        function setEmailError(msg){
            emailInput.classList.add('is-invalid');
            emailFeedback.textContent=msg;
        }
        function clearEmailError(){
            emailInput.classList.remove('is-invalid');
            emailFeedback.textContent='';
        }
        function startCooldown(seconds){
            cooldown=seconds;
            updateCountdown();
            if(timer) clearInterval(timer);
            timer=setInterval(()=>{
                cooldown--;
                updateCountdown();
                if(cooldown<=0){clearInterval(timer);}
            },1000);
        }
        function updateCountdown(){
            countdownEl.textContent=cooldown<0?0:cooldown;
            if(cooldown>0) resendBtn.classList.add('disabled');
            else resendBtn.classList.remove('disabled');
        }
        function collectOtp(){
            let c=''; otpInputs.forEach(i=>c+=i.value.trim()); return c;
        }

        /* ---------- STEP 1: Check + Send OTP ---------- */
        checkEmailBtn.addEventListener('click', async ()=>{
            const email = emailInput.value.trim().toLowerCase();
            if(!email){ setEmailError('Vui lòng nhập email.'); return; }
            if(!validateEmailFormat(email)){ setEmailError('Email không hợp lệ.'); return; }
            clearEmailError();
            emailStatus.textContent='';
            checkEmailBtn.disabled=true;
            checkEmailBtn.innerHTML='<span class="spinner-border spinner-border-sm me-2"></span>Đang xử lý...';

            try{
                // 1. CheckEmail
                const chk = await postJson('/Account/CheckEmail',{ Email: email });
                if(chk.exists){
                    setEmailError('Email này đã được đăng ký.');
                    checkEmailBtn.disabled=false;
                    checkEmailBtn.textContent='Kiểm tra & Gửi mã OTP';
                    return;
                }
                // 2. SendOtp
                const send = await postJson('/Account/SendOtp',{ Email: email });
                if(!send.success){
                    setEmailError(send.message || 'Không gửi được OTP.');
                    checkEmailBtn.disabled=false;
                    checkEmailBtn.textContent='Kiểm tra & Gửi mã OTP';
                    return;
                }

                currentEmail = email;
                userEmailEl.textContent=email;
                finalEmail.value=email;
                otpInputs.forEach(i=>i.value='');
                otpFeedback.style.display='none';
                otpVerified=false;
                startCooldown(send.cooldown || 30);
                showToast('OTP đã gửi','success');
                showStep(2);
            }catch(err){
                setEmailError('Lỗi hệ thống: '+err.message);
            }finally{
                checkEmailBtn.disabled=false;
                checkEmailBtn.textContent='Kiểm tra & Gửi mã OTP';
            }
        });

        /* ---------- OTP input auto move ---------- */
        otpInputs.forEach(inp=>{
            inp.addEventListener('input', e=>{
                e.target.value=e.target.value.replace(/\\D/g,'').slice(0,1);
                const idx=parseInt(e.target.dataset.index);
                if(e.target.value && idx<6){
                    const next=document.querySelector('.otp-input[data-index="'+(idx+1)+'"]');
                    next && next.focus();
                }
            });
            inp.addEventListener('keydown', e=>{
                if(e.key==='Backspace' && !e.target.value){
                    const idx=parseInt(e.target.dataset.index);
                    if(idx>1){
                        const prev=document.querySelector('.otp-input[data-index="'+(idx-1)+'"]');
                        prev && prev.focus();
                    }
                }
            });
        });

        /* ---------- Resend OTP ---------- */
        resendBtn.addEventListener('click', async e=>{
            e.preventDefault();
            if(cooldown>0 || !currentEmail) return;
            resendBtn.classList.add('disabled');
            try{
                const r = await postJson('/Account/ResendOtp',{ Email: currentEmail });
                if(r.success){
                    showToast('Đã gửi lại OTP.','success');
                    startCooldown(r.cooldown || 30);
                }else{
                    showToast(r.message || 'Không gửi lại OTP được.','error');
                    resendBtn.classList.remove('disabled');
                }
            }catch(err){
                showToast('Lỗi: '+err.message,'error');
                resendBtn.classList.remove('disabled');
            }
        });

        backToEmailBtn.addEventListener('click', ()=>{
            currentEmail='';
            showStep(1);
        });

        /* ---------- Verify OTP ---------- */
        verifyOtpBtn.addEventListener('click', async ()=>{
            const code = collectOtp();
            if(code.length!==6){
                otpFeedback.textContent='Vui lòng nhập đủ 6 số.';
                otpFeedback.style.display='block';
                return;
            }
            otpFeedback.style.display='none';
            verifyOtpBtn.disabled=true;
            verifyOtpBtn.innerHTML='<span class="spinner-border spinner-border-sm me-2"></span>Đang kiểm tra...';
            try{
                const res = await postJson('/Account/VerifyOtpAjax',{ Email: currentEmail, Otp: code });
                if(!res.success){
                    otpFeedback.textContent = res.message || 'OTP không đúng.';
                    otpFeedback.style.display='block';
                }else{
                    otpVerified=true;
                    showToast('OTP hợp lệ.','success');
                    showStep(3);
                }
            }catch(err){
                otpFeedback.textContent='Lỗi hệ thống: '+err.message;
                otpFeedback.style.display='block';
            }finally{
                verifyOtpBtn.disabled=false;
                verifyOtpBtn.textContent='Xác thực OTP';
            }
        });

        backToOtpBtn.addEventListener('click', ()=>{
            if(!currentEmail){ showStep(1); return; }
            showStep(2);
        });

        /* ---------- Validate details ---------- */
        function markInvalid(el,msg){
            el.classList.add('is-invalid');
            const fb = el.parentElement.querySelector('.invalid-feedback');
            if(fb) fb.textContent=msg;
        }
        function clearInvalid(el){
            el.classList.remove('is-invalid');
        }
        function validateDetails(){
            let ok=true;
            if(!hoTenInput.value.trim()){ markInvalid(hoTenInput,'Vui lòng nhập họ tên'); ok=false; } else clearInvalid(hoTenInput);
            if(!tenDangNhapInput.value.trim()){ markInvalid(tenDangNhapInput,'Vui lòng nhập tên đăng nhập'); ok=false; } else clearInvalid(tenDangNhapInput);
            if(passwordInput.value.length<8){ markInvalid(passwordInput,'Mật khẩu tối thiểu 8 ký tự'); ok=false; } else clearInvalid(passwordInput);
            if(confirmPasswordInput.value!==passwordInput.value || !confirmPasswordInput.value){
                markInvalid(confirmPasswordInput,'Mật khẩu xác nhận không khớp'); ok=false;
            } else clearInvalid(confirmPasswordInput);
            if(!termsCheckbox.checked){ showToast('Bạn phải đồng ý điều khoản.','error'); ok=false; }
            if(!otpVerified){ showToast('Bạn chưa xác thực OTP.','error'); ok=false; }
            return ok;
        }

        /* ---------- Complete Registration ---------- */
        submitRegisterBtn.addEventListener('click', async ()=>{
            if(!validateDetails()) return;
            submitRegisterBtn.disabled=true;
            submitRegisterBtn.innerHTML='<span class="spinner-border spinner-border-sm me-2"></span>Đang tạo...';
            try{
                // Gửi multipart hay JSON? Action CompleteRegistration nhận model + [ValidateAntiForgeryToken] => dùng FormData
                const fd = new FormData(finalForm);
                fd.set('Email', finalEmail.value); // đảm bảo
                // Thêm token (AntiForgery)
                const token = finalForm.querySelector('input[name="__RequestVerificationToken"]');
                const res = await fetch('/Account/CompleteRegistration',{
                    method:'POST',
                    headers: token ? { 'RequestVerificationToken': token.value } : {},
                    body: fd
                }).then(r=>r.json());

                if(!res.success){
                    showToast(res.message || 'Đăng ký thất bại.','error');
                }else{
                    showToast('Đăng ký thành công!','success');
                    showStep(4);
                }
            }catch(err){
                showToast('Lỗi hệ thống: '+err.message,'error');
            }finally{
                submitRegisterBtn.disabled=false;
                submitRegisterBtn.textContent='Hoàn tất đăng ký';
            }
        });

        /* ---------- Password instant check ---------- */
        passwordInput.addEventListener('input', ()=>{
            if(passwordInput.value && passwordInput.value.length<8) passwordInput.classList.add('is-invalid');
            else passwordInput.classList.remove('is-invalid');
            if(confirmPasswordInput.value && passwordInput.value!==confirmPasswordInput.value)
                confirmPasswordInput.classList.add('is-invalid');
            else confirmPasswordInput.classList.remove('is-invalid');
        });
        confirmPasswordInput.addEventListener('input', ()=>{
            if(confirmPasswordInput.value!==passwordInput.value) confirmPasswordInput.classList.add('is-invalid');
            else confirmPasswordInput.classList.remove('is-invalid');
        });
