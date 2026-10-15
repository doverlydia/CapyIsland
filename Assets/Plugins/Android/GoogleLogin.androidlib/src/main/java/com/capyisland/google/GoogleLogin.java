package com.capyisland.google;

import android.app.Activity;
import android.os.CancellationSignal;

import java.util.concurrent.Executor;

import androidx.credentials.Credential;
import androidx.credentials.CredentialManager;
import androidx.credentials.CredentialManagerCallback;
import androidx.credentials.CustomCredential;
import androidx.credentials.GetCredentialRequest;
import androidx.credentials.GetCredentialResponse;
import androidx.credentials.exceptions.GetCredentialCancellationException;
import androidx.credentials.exceptions.GetCredentialException;
import androidx.credentials.exceptions.NoCredentialException;

import com.google.android.libraries.identity.googleid.GetSignInWithGoogleOption;
import com.google.android.libraries.identity.googleid.GoogleIdTokenCredential;

/**
 * Shows Google's own "choose an account" sheet and hands back an ID token, which the game passes
 * to Unity Authentication. GoogleLogin.cs calls this through JNI.
 */
public final class GoogleLogin {
    /** Answers back to C# (an AndroidJavaProxy). */
    public interface Listener {
        void onToken(String idToken);
        /** kind: "cancel", "none" (no Google account on the phone) or "error". */
        void onError(String kind, String message);
    }

    private GoogleLogin() {}

    public static void signIn(final Activity activity, final String webClientId, final Listener listener) {
        activity.runOnUiThread(new Runnable() {
            @Override public void run() { ask(activity, webClientId, listener); }
        });
    }

    private static void ask(final Activity activity, String webClientId, final Listener listener) {
        try {
            GetSignInWithGoogleOption option = new GetSignInWithGoogleOption.Builder(webClientId).build();
            GetCredentialRequest request = new GetCredentialRequest.Builder().addCredentialOption(option).build();
            CredentialManager.create(activity).getCredentialAsync(activity, request, new CancellationSignal(),
                new Executor() { @Override public void execute(Runnable r) { activity.runOnUiThread(r); } },
                new CredentialManagerCallback<GetCredentialResponse, GetCredentialException>() {
                    @Override public void onResult(GetCredentialResponse response) {
                        Credential credential = response.getCredential();
                        if (credential instanceof CustomCredential
                                && GoogleIdTokenCredential.TYPE_GOOGLE_ID_TOKEN_CREDENTIAL.equals(credential.getType())) {
                            try {
                                listener.onToken(GoogleIdTokenCredential.createFrom(credential.getData()).getIdToken());
                            } catch (Exception e) {
                                listener.onError("error", String.valueOf(e.getMessage()));
                            }
                        } else {
                            listener.onError("error", "Unexpected credential: " + credential.getType());
                        }
                    }

                    @Override public void onError(GetCredentialException e) {
                        String kind = e instanceof GetCredentialCancellationException ? "cancel"
                            : e instanceof NoCredentialException ? "none" : "error";
                        listener.onError(kind, e.getType() + ": " + e.getMessage());
                    }
                });
        } catch (Exception e) {
            listener.onError("error", String.valueOf(e.getMessage()));
        }
    }
}
