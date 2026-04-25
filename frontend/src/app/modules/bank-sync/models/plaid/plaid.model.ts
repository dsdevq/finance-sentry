export interface PlaidSuccessMetadata {
  institution: Nullable<{name: string; institution_id: string}>;
  accounts: {id: string; name: string; mask: string; type: string; subtype: string}[];
  link_session_id: string;
}

export interface PlaidLinkOptions {
  token: string;
  onSuccess: (publicToken: string, metadata: PlaidSuccessMetadata) => void;
  onExit?: (err: unknown, metadata: unknown) => void;
  onLoad?: () => void;
  onEvent?: (eventName: string, metadata: unknown) => void;
}

export interface PlaidHandler {
  open: () => void;
  destroy: () => void;
}

export interface PreparePlaidOptions {
  token: string;
  onSuccess: (publicToken: string, metadata: PlaidSuccessMetadata) => void;
  onExit?: (err: unknown) => void;
}
